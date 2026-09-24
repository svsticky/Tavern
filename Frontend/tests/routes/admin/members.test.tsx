import { configure } from "@testing-library/dom";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { useLocation } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { MemberResponseDto } from "~/api";
import { renderWithProviders } from "~/testUtils";

// The component debounces search refetches by 300ms; under full-suite parallel load the
// default 1000ms async-query timeout can be too tight, so give these queries more headroom.
configure({ asyncUtilTimeout: 20000 });
vi.setConfig({ testTimeout: 25000 });

const { fetchMembersPage } = vi.hoisted(() => ({
  fetchMembersPage: vi.fn(),
}));
vi.mock("~/routes/admin/members.handlers", async (importOriginal) => ({
  ...(await importOriginal<typeof import("~/routes/admin/members.handlers")>()),
  fetchMembersPage,
}));

const { requireTokenParsed } = vi.hoisted(() => ({
  requireTokenParsed: vi.fn(),
}));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

const { useLoaderData } = vi.hoisted(() => ({
  useLoaderData: vi.fn(),
}));
vi.mock("react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("react-router")>()),
  useLoaderData,
}));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: { error: (...args: unknown[]) => toastErrorFn(...args) },
}));

vi.mock("~/components/Member/FilterMemberOverlay/FilterMemberOverlay", () => ({
  default: ({ onFilter }: { onFilter: (f: any) => void }) => (
    <button type="button" onClick={() => onFilter({ studyId: 5 })}>
      apply-filters
    </button>
  ),
}));

// jsdom does not implement IntersectionObserver. Stub it locally (not touching the shared
// vitest.setup.ts) and capture the callback so tests can simulate the loader coming into view.
let intersectionCallback: IntersectionObserverCallback | null = null;
class IntersectionObserverStub {
  constructor(callback: IntersectionObserverCallback) {
    intersectionCallback = callback;
  }
  observe() {}
  unobserve() {}
  disconnect() {}
}

function makeMembers(count: number, offset = 0): MemberResponseDto[] {
  return Array.from({ length: count }, (_, i) => ({
    id: `member-${offset + i}`,
    firstName: `First${offset + i}`,
    lastName: `Last${offset + i}`,
    email: `member${offset + i}@example.com`,
    phoneNumber: "0612345678",
  })) as MemberResponseDto[];
}

function loaderData(
  overrides: Partial<ReturnType<typeof baseLoaderData>> = {},
) {
  return { ...baseLoaderData(), ...overrides };
}
function baseLoaderData() {
  return {
    members: [] as MemberResponseDto[],
    hasMore: false,
    search: "",
    filters: null as Record<string, unknown> | null,
  };
}

function LocationProbe() {
  return <div data-testid="location">{useLocation().search}</div>;
}

function loaderRequest(search = "") {
  return { request: new Request(`http://localhost/admin/members${search}`) };
}

import Members, { clientLoader } from "~/routes/admin/members";

describe("admin members clientLoader", () => {
  beforeEach(() => vi.clearAllMocks());

  it("fetches the first, unfiltered page of members", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    fetchMembersPage.mockResolvedValue(makeMembers(20));

    const result = await clientLoader(loaderRequest());

    expect(fetchMembersPage).toHaveBeenCalledWith(1, "", null);
    expect(result.members).toHaveLength(20);
    expect(result.hasMore).toBe(true);
    expect(result.search).toBe("");
    expect(result.filters).toBeNull();
  });

  it("reports hasMore as false for a partial page", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    fetchMembersPage.mockResolvedValue(makeMembers(5));

    const result = await clientLoader(loaderRequest());

    expect(result.hasMore).toBe(false);
  });

  it("rebuilds the search, filters and every loaded page from the URL", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    fetchMembersPage.mockResolvedValue(makeMembers(20));

    const result = await clientLoader(
      loaderRequest(
        `?q=jane&filters=${encodeURIComponent('{"studyId":5}')}&pages=3`,
      ),
    );

    const expectedFilters = expect.objectContaining({ studyId: 5 });
    expect(fetchMembersPage).toHaveBeenCalledTimes(3);
    expect(fetchMembersPage).toHaveBeenCalledWith(1, "jane", expectedFilters);
    expect(fetchMembersPage).toHaveBeenCalledWith(2, "jane", expectedFilters);
    expect(fetchMembersPage).toHaveBeenCalledWith(3, "jane", expectedFilters);
    expect(result.members).toHaveLength(60);
    expect(result.hasMore).toBe(true);
    expect(result.search).toBe("jane");
    expect(result.filters?.studyId).toBe(5);
  });

  it("treats malformed filters in the URL as no filters", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    fetchMembersPage.mockResolvedValue([]);

    await clientLoader(loaderRequest("?filters=%7Bnot-json"));

    expect(fetchMembersPage).toHaveBeenCalledWith(1, "", null);
  });

  it("propagates a fetch failure to React Router's error boundary", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    fetchMembersPage.mockRejectedValue(new Error("fail"));

    await expect(clientLoader(loaderRequest())).rejects.toThrow("fail");
  });
});

describe("Members", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("IntersectionObserver", IntersectionObserverStub);
    intersectionCallback = null;
  });

  it("renders the members the loader already fetched", () => {
    useLoaderData.mockReturnValue(
      loaderData({ members: makeMembers(20), hasMore: true }),
    );

    renderWithProviders(<Members />);

    expect(screen.getByText("First0 Last0")).toBeInTheDocument();
    expect(fetchMembersPage).not.toHaveBeenCalled();
  });

  it("shows an error toast when loading more fails", async () => {
    useLoaderData.mockReturnValue(
      loaderData({ members: makeMembers(20), hasMore: true }),
    );
    fetchMembersPage.mockRejectedValue(new Error("fail"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderWithProviders(<Members />);
    intersectionCallback!(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );

    await waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("restores the search text from the loader data", () => {
    useLoaderData.mockReturnValue(loaderData({ search: "jane" }));

    renderWithProviders(<Members />);

    expect(screen.getByLabelText("search")).toHaveValue("jane");
  });

  it("shows 'no_data' when the loader found no members", () => {
    useLoaderData.mockReturnValue(loaderData());

    renderWithProviders(<Members />);

    expect(screen.getByText("no_data")).toBeInTheDocument();
  });

  it("debounces the search query before writing it to the URL", async () => {
    useLoaderData.mockReturnValue(loaderData());
    renderWithProviders(
      <>
        <Members />
        <LocationProbe />
      </>,
    );

    fireEvent.change(screen.getByLabelText("search"), {
      target: { value: "jane" },
    });

    expect(screen.getByTestId("location")).toHaveTextContent("");

    await waitFor(
      () => expect(screen.getByTestId("location")).toHaveTextContent("?q=jane"),
      { timeout: 2000 },
    );
  });

  it("loads more members when the loader comes into view and records the page in the URL", async () => {
    useLoaderData.mockReturnValue(
      loaderData({ members: makeMembers(20, 0), hasMore: true }),
    );
    fetchMembersPage.mockResolvedValue(makeMembers(5, 20));

    renderWithProviders(
      <>
        <Members />
        <LocationProbe />
      </>,
    );
    expect(intersectionCallback).toBeTruthy();

    intersectionCallback!(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );

    expect(await screen.findByText("First20 Last20")).toBeInTheDocument();
    expect(fetchMembersPage).toHaveBeenCalledWith(2, "", null);
    await waitFor(() =>
      expect(screen.getByTestId("location")).toHaveTextContent("?pages=2"),
    );
  });

  it("opens the filters modal and writes the applied filters to the URL, restarting at page 1", async () => {
    useLoaderData.mockReturnValue(loaderData());

    renderWithProviders(
      <>
        <Members />
        <LocationProbe />
      </>,
      { route: "/admin/members?pages=4" },
    );
    fireEvent.click(screen.getByText("filters"));
    fireEvent.click(await screen.findByText("apply-filters"));

    await waitFor(() => {
      const search = screen.getByTestId("location").textContent ?? "";
      const params = new URLSearchParams(search);
      expect(JSON.parse(params.get("filters") ?? "{}")).toEqual({ studyId: 5 });
      expect(params.has("pages")).toBe(false);
    });
  });

  it("navigates to create-member when the plus button is clicked", () => {
    useLoaderData.mockReturnValue(loaderData());

    renderWithProviders(<Members />);
    const plusButton = document
      .querySelector("svg.lucide-plus")
      ?.closest("button");
    expect(plusButton).toBeTruthy();
    fireEvent.click(plusButton!);
  });
});
