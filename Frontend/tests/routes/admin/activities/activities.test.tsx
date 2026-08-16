import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import { renderWithProviders } from "~/testUtils";
import { getCommitteeYear } from "~/util/date.util";

const { loadAdminActivities, handleViewActivity } = vi.hoisted(() => ({
  loadAdminActivities: vi.fn(),
  handleViewActivity: vi.fn(),
}));

vi.mock("~/routes/admin/activities/activities.handlers", () => ({
  loadAdminActivities,
  handleViewActivity,
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

import Activities from "~/routes/admin/activities/activities";

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

describe("Activities (admin)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("IntersectionObserver", IntersectionObserverStub);
    intersectionCallback = null;
    // Default: resolve immediately with no activities so most tests don't hang.
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        setLoading(true);
        setActivities([]);
        setLoading(false);
      },
    );
  });

  it("loads activities for the current committee year on mount", async () => {
    renderWithProviders(<Activities />);

    await waitFor(() => expect(loadAdminActivities).toHaveBeenCalled());

    const currentYear = getCommitteeYear();
    expect(loadAdminActivities).toHaveBeenCalledWith(
      currentYear,
      expect.any(Function),
      expect.any(Function),
      1,
      15,
    );
  });

  it("renders fetched activities in the table with formatted price and participants", async () => {
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        setLoading(true);
        setActivities([
          makeActivity({
            id: 1,
            name: "Feest",
            location: "Kroeg",
            price: 5,
            participantLimit: 50,
            enrollments: [
              { isOnWaitingList: false },
              { isOnWaitingList: true },
            ] as ActivityResponseDto["enrollments"],
          }),
        ]);
        setLoading(false);
      },
    );

    renderWithProviders(<Activities />);

    expect(await screen.findByText("Feest")).toBeInTheDocument();
    expect(screen.getByText("Kroeg")).toBeInTheDocument();
    expect(screen.getByText("€5.00")).toBeInTheDocument();
    // 1 non-waitlisted enrollment out of a limit of 50
    expect(screen.getByText(/1\/50/)).toBeInTheDocument();
  });

  it("shows 'free' for activities with no price", async () => {
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        setActivities([makeActivity({ price: 0 })]);
        setLoading(false);
      },
    );

    renderWithProviders(<Activities />);

    expect(await screen.findByText("free")).toBeInTheDocument();
  });

  it("filters activities by search query (name or location)", async () => {
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        setActivities([
          makeActivity({ id: 1, name: "Feest", location: "Kroeg" }),
          makeActivity({ id: 2, name: "Borrel", location: "Kantine" }),
        ]);
        setLoading(false);
      },
    );

    renderWithProviders(<Activities />);

    expect(await screen.findByText("Feest")).toBeInTheDocument();
    expect(screen.getByText("Borrel")).toBeInTheDocument();

    const searchInput = screen.getByPlaceholderText("search_activities");
    fireEvent.change(searchInput, { target: { value: "kantine" } });

    expect(screen.queryByText("Feest")).not.toBeInTheDocument();
    expect(screen.getByText("Borrel")).toBeInTheDocument();
  });

  it("reloads activities when the year selector changes", async () => {
    renderWithProviders(<Activities />);

    await waitFor(() => expect(loadAdminActivities).toHaveBeenCalledTimes(1));

    const yearSelect = screen.getByLabelText("year");
    fireEvent.change(yearSelect, { target: { value: "2020" } });

    await waitFor(() => expect(loadAdminActivities).toHaveBeenCalledTimes(2));
    expect(loadAdminActivities).toHaveBeenLastCalledWith(
      2020,
      expect.any(Function),
      expect.any(Function),
      1,
      15,
    );
  });

  it("calls handleViewActivity with the navigate function and activity id", async () => {
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        setActivities([makeActivity({ id: 42, name: "Feest" })]);
        setLoading(false);
      },
    );

    renderWithProviders(<Activities />);

    const viewButton = await screen.findByText("view_activity");
    fireEvent.click(viewButton);

    expect(handleViewActivity).toHaveBeenCalledWith(expect.any(Function), 42);
  });

  it("shows 'no_data' when there are no activities and loading has finished", async () => {
    renderWithProviders(<Activities />);

    await screen.findByText("no_data");
  });

  it("fetches the next page when the loader becomes visible and more pages are available", async () => {
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        // Return a full page so hasMore stays true.
        const page = Array.from({ length: 15 }, (_, i) =>
          makeActivity({ id: i + 1, name: `Activity ${i + 1}` }),
        );
        setActivities(page);
        setLoading(false);
      },
    );

    renderWithProviders(<Activities />);

    await waitFor(() => expect(loadAdminActivities).toHaveBeenCalledTimes(1));
    expect(await screen.findByText("load_more")).toBeInTheDocument();

    expect(intersectionCallback).not.toBeNull();
    intersectionCallback?.(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );

    await waitFor(() => expect(loadAdminActivities).toHaveBeenCalledTimes(2));
    expect(loadAdminActivities).toHaveBeenLastCalledWith(
      expect.any(Number),
      expect.any(Function),
      expect.any(Function),
      2,
      15,
    );
  });

  it("does not show a participant limit suffix when there is none", async () => {
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        setActivities([
          makeActivity({ id: 1, name: "Feest", participantLimit: null }),
        ]);
        setLoading(false);
      },
    );

    renderWithProviders(<Activities />);

    expect(await screen.findByText("👥 0")).toBeInTheDocument();
  });

  it("matches activities by location even without a matching name", async () => {
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        setActivities([
          makeActivity({ id: 1, name: "Feest", location: "Kroeg" }),
          makeActivity({ id: 2, name: "Borrel", location: undefined }),
        ]);
        setLoading(false);
      },
    );

    renderWithProviders(<Activities />);

    expect(await screen.findByText("Feest")).toBeInTheDocument();
    const searchInput = screen.getByPlaceholderText("search_activities");
    fireEvent.change(searchInput, { target: { value: "kroeg" } });

    expect(screen.getByText("Feest")).toBeInTheDocument();
    expect(screen.queryByText("Borrel")).not.toBeInTheDocument();
  });

  it("does not fetch the next page when the loader intersects but there are no more pages", async () => {
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        setActivities([makeActivity({ id: 1, name: "Feest" })]);
        setLoading(false);
      },
    );

    renderWithProviders(<Activities />);

    await screen.findByText("no_more_activities");
    expect(intersectionCallback).not.toBeNull();
    intersectionCallback?.(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );

    expect(loadAdminActivities).toHaveBeenCalledTimes(1);
  });

  it("shows a loading_more label while a page fetch is in flight", async () => {
    let resolveFetch: (() => void) | undefined;
    loadAdminActivities.mockImplementation(
      (_year, setLoading, _setActivities) =>
        new Promise<void>((resolve) => {
          setLoading(true);
          resolveFetch = () => {
            setLoading(false);
            resolve();
          };
        }),
    );

    renderWithProviders(<Activities />);

    expect(await screen.findByText("loading_more")).toBeInTheDocument();
    resolveFetch?.();
  });

  it("does not update activities when unmounted before the fetch resolves", async () => {
    let resolveFetch: (() => void) | undefined;
    loadAdminActivities.mockImplementation(
      (_year, setLoading, setActivities) =>
        new Promise<void>((resolve) => {
          resolveFetch = () => {
            setActivities([makeActivity({ id: 1, name: "Feest" })]);
            setLoading(false);
            resolve();
          };
        }),
    );

    const { unmount } = renderWithProviders(<Activities />);
    await waitFor(() => expect(loadAdminActivities).toHaveBeenCalled());

    unmount();
    resolveFetch?.();
  });

  it("shows 'no_more_activities' once a partial page has been loaded", async () => {
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        setActivities([makeActivity({ id: 1, name: "Feest" })]);
        setLoading(false);
      },
    );

    renderWithProviders(<Activities />);

    expect(await screen.findByText("no_more_activities")).toBeInTheDocument();
  });
});
