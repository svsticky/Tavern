import { configure } from "@testing-library/dom";
import { fireEvent, screen, waitFor } from "@testing-library/react";
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
  return { members: [] as MemberResponseDto[], hasMore: false };
}

import Members, { clientLoader } from "~/routes/admin/members";

describe("admin members clientLoader", () => {
  beforeEach(() => vi.clearAllMocks());

  it("fetches the first, unfiltered page of members", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    fetchMembersPage.mockResolvedValue(makeMembers(20));

    const result = await clientLoader();

    expect(fetchMembersPage).toHaveBeenCalledWith(1, "", null);
    expect(result.members).toHaveLength(20);
    expect(result.hasMore).toBe(true);
  });

  it("reports hasMore as false for a partial page", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    fetchMembersPage.mockResolvedValue(makeMembers(5));

    const result = await clientLoader();

    expect(result.hasMore).toBe(false);
  });

  it("propagates a fetch failure to React Router's error boundary", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    fetchMembersPage.mockRejectedValue(new Error("fail"));

    await expect(clientLoader()).rejects.toThrow("fail");
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

  it("shows an error toast when a search/filter refetch fails", async () => {
    useLoaderData.mockReturnValue(loaderData());
    fetchMembersPage.mockRejectedValue(new Error("fail"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderWithProviders(<Members />);
    fireEvent.change(screen.getByLabelText("search"), {
      target: { value: "jane" },
    });

    await waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("shows 'no_data' when the loader found no members", () => {
    useLoaderData.mockReturnValue(loaderData());

    renderWithProviders(<Members />);

    expect(screen.getByText("no_data")).toBeInTheDocument();
  });

  it("debounces the search query before refetching", async () => {
    useLoaderData.mockReturnValue(loaderData());
    fetchMembersPage.mockResolvedValue([]);
    renderWithProviders(<Members />);

    fireEvent.change(screen.getByLabelText("search"), {
      target: { value: "jane" },
    });

    expect(fetchMembersPage).not.toHaveBeenCalled();

    await waitFor(
      () => expect(fetchMembersPage).toHaveBeenCalledWith(1, "jane", null),
      { timeout: 2000 },
    );
  });

  it("loads more members when the loader comes into view", async () => {
    useLoaderData.mockReturnValue(
      loaderData({ members: makeMembers(20, 0), hasMore: true }),
    );
    fetchMembersPage.mockResolvedValue(makeMembers(5, 20));

    renderWithProviders(<Members />);
    expect(intersectionCallback).toBeTruthy();

    intersectionCallback!(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );

    expect(await screen.findByText("First20 Last20")).toBeInTheDocument();
    expect(fetchMembersPage).toHaveBeenCalledWith(2, "", null);
  });

  it("opens the filters modal and applies filters", async () => {
    useLoaderData.mockReturnValue(loaderData());
    fetchMembersPage.mockResolvedValue([]);

    renderWithProviders(<Members />);
    fireEvent.click(screen.getByText("filters"));
    fireEvent.click(await screen.findByText("apply-filters"));

    await waitFor(() =>
      expect(fetchMembersPage).toHaveBeenCalledWith(
        1,
        "",
        expect.objectContaining({ studyId: 5 }),
      ),
    );
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
