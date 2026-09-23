import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import { renderWithProviders } from "~/testUtils";
import { getCommitteeYear } from "~/util/date.util";

const { fetchAdminActivitiesPage, handleViewActivity } = vi.hoisted(() => ({
  fetchAdminActivitiesPage: vi.fn(),
  handleViewActivity: vi.fn(),
}));
vi.mock("~/routes/admin/activities/activities.handlers", () => ({
  fetchAdminActivitiesPage,
  handleViewActivity,
}));

const { requireTokenParsed } = vi.hoisted(() => ({
  requireTokenParsed: vi.fn(),
}));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

const { useLoaderData, setSearchParams } = vi.hoisted(() => ({
  useLoaderData: vi.fn(),
  setSearchParams: vi.fn(),
}));
vi.mock("react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("react-router")>()),
  useLoaderData,
  useSearchParams: () => [new URLSearchParams(), setSearchParams],
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

import Activities, { clientLoader } from "~/routes/admin/activities/activities";

function makeActivity(
  overrides: Partial<ActivityResponseDto> = {},
): ActivityResponseDto {
  return {
    id: 1,
    name: "Feest",
    location: "Kroeg",
    dateTimeStart: "2026-01-01T20:00:00Z",
    price: 5,
    participantLimit: 50,
    enrollments: [],
    ...overrides,
  } as ActivityResponseDto;
}

function loaderData(
  overrides: Partial<ReturnType<typeof baseLoaderData>> = {},
) {
  return { ...baseLoaderData(), ...overrides };
}
function baseLoaderData() {
  return {
    activities: [] as ActivityResponseDto[],
    year: getCommitteeYear(),
    search: "",
    hasMore: false,
  };
}

describe("admin activities clientLoader", () => {
  it("defaults to the current committee year and empty search when the URL has none", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    fetchAdminActivitiesPage.mockResolvedValue([]);

    const result = await clientLoader({
      request: new Request("https://example.com/admin/activities"),
    });

    const currentYear = getCommitteeYear();
    expect(fetchAdminActivitiesPage).toHaveBeenCalledWith(
      currentYear,
      1,
      15,
      "",
    );
    expect(result).toEqual({
      activities: [],
      year: currentYear,
      search: "",
      hasMore: false,
    });
  });

  it("reads year/search from the URL and reports hasMore for a full page", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    const page = Array.from({ length: 15 }, (_, i) =>
      makeActivity({ id: i + 1 }),
    );
    fetchAdminActivitiesPage.mockResolvedValue(page);

    const result = await clientLoader({
      request: new Request(
        "https://example.com/admin/activities?year=2020&search=kroeg",
      ),
    });

    expect(fetchAdminActivitiesPage).toHaveBeenCalledWith(2020, 1, 15, "kroeg");
    expect(result.year).toBe(2020);
    expect(result.search).toBe("kroeg");
    expect(result.hasMore).toBe(true);
  });
});

describe("Activities (admin)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("IntersectionObserver", IntersectionObserverStub);
    intersectionCallback = null;
  });

  it("renders fetched activities in the table with formatted price and participants", () => {
    useLoaderData.mockReturnValue(
      loaderData({
        activities: [
          makeActivity({
            enrollments: [
              { isOnWaitingList: false },
              { isOnWaitingList: true },
            ] as ActivityResponseDto["enrollments"],
          }),
        ],
      }),
    );

    renderWithProviders(<Activities />);

    expect(screen.getByText("Feest")).toBeInTheDocument();
    expect(screen.getByText("Kroeg")).toBeInTheDocument();
    expect(screen.getByText("€5.00")).toBeInTheDocument();
    expect(screen.getByText(/1\/50/)).toBeInTheDocument();
  });

  it("shows 'free' for activities with no price", () => {
    useLoaderData.mockReturnValue(
      loaderData({ activities: [makeActivity({ price: 0 })] }),
    );

    renderWithProviders(<Activities />);

    expect(screen.getByText("free")).toBeInTheDocument();
  });

  it("does not show a participant limit suffix when there is none", () => {
    useLoaderData.mockReturnValue(
      loaderData({
        activities: [makeActivity({ participantLimit: null, enrollments: [] })],
      }),
    );

    renderWithProviders(<Activities />);

    expect(screen.getByText("👥 0")).toBeInTheDocument();
  });

  it("calls handleViewActivity with the navigate function and activity id", () => {
    useLoaderData.mockReturnValue(
      loaderData({ activities: [makeActivity({ id: 42 })] }),
    );

    renderWithProviders(<Activities />);

    fireEvent.click(screen.getByText("view_activity"));

    expect(handleViewActivity).toHaveBeenCalledWith(expect.any(Function), 42);
  });

  it("shows 'no_data' when there are no activities", () => {
    useLoaderData.mockReturnValue(loaderData());

    renderWithProviders(<Activities />);

    expect(screen.getByText("no_data")).toBeInTheDocument();
  });

  it("shows 'no_more_activities' when a partial page has loaded", () => {
    useLoaderData.mockReturnValue(
      loaderData({ activities: [makeActivity()], hasMore: false }),
    );

    renderWithProviders(<Activities />);

    expect(screen.getByText("no_more_activities")).toBeInTheDocument();
  });

  it("shows 'load_more' when there are more pages available", () => {
    useLoaderData.mockReturnValue(
      loaderData({ activities: [makeActivity()], hasMore: true }),
    );

    renderWithProviders(<Activities />);

    expect(screen.getByText("load_more")).toBeInTheDocument();
  });

  it("updates the URL's search param (debounced) as the user types", async () => {
    useLoaderData.mockReturnValue(loaderData());
    renderWithProviders(<Activities />);

    fireEvent.change(screen.getByPlaceholderText("search_activities"), {
      target: { value: "kantine" },
    });

    expect(setSearchParams).not.toHaveBeenCalled();

    await waitFor(() => expect(setSearchParams).toHaveBeenCalled(), {
      timeout: 1000,
    });
    const [params] = setSearchParams.mock.calls.at(-1)!;
    expect((params as URLSearchParams).get("search")).toBe("kantine");
  });

  it("updates the URL's year param when the year selector changes", () => {
    useLoaderData.mockReturnValue(loaderData());
    renderWithProviders(<Activities />);

    fireEvent.change(screen.getByLabelText("year"), {
      target: { value: "2020" },
    });

    const [params] = setSearchParams.mock.calls.at(-1)!;
    expect((params as URLSearchParams).get("year")).toBe("2020");
  });

  it("fetches the next page when the loader becomes visible and more pages are available", async () => {
    const page = Array.from({ length: 15 }, (_, i) =>
      makeActivity({ id: i + 1, name: `Activity ${i + 1}` }),
    );
    useLoaderData.mockReturnValue(
      loaderData({ activities: page, hasMore: true }),
    );
    fetchAdminActivitiesPage.mockResolvedValue([]);

    renderWithProviders(<Activities />);
    expect(screen.getByText("load_more")).toBeInTheDocument();

    expect(intersectionCallback).not.toBeNull();
    intersectionCallback?.(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );

    await waitFor(() =>
      expect(fetchAdminActivitiesPage).toHaveBeenCalledWith(
        getCommitteeYear(),
        2,
        15,
        "",
      ),
    );
  });

  it("does not fetch the next page when there are no more pages", () => {
    useLoaderData.mockReturnValue(
      loaderData({ activities: [makeActivity()], hasMore: false }),
    );

    renderWithProviders(<Activities />);
    intersectionCallback?.(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );

    expect(fetchAdminActivitiesPage).not.toHaveBeenCalled();
  });
});
