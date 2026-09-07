import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";
import { getCommitteeYear } from "~/util/date.util";

const boardAuthService = createMockAuthService({
  getTokenParsed: vi.fn(
    async () =>
      ({
        locale: "en",
        UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
        access_level: "member",
        given_name: "Board",
        family_name: "Member",
        name: "Board Member",
        is_admin: true,
      }) satisfies TokenParsed,
  ),
});

const { loadAdminActivities, handleViewActivity, handleDeleteAdminActivity } =
  vi.hoisted(() => ({
    loadAdminActivities: vi.fn(),
    handleViewActivity: vi.fn(),
    handleDeleteAdminActivity: vi.fn(),
  }));

vi.mock("~/routes/admin/activities/activities.handlers", () => ({
  loadAdminActivities,
  handleViewActivity,
  handleDeleteAdminActivity,
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
      false,
      false,
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
      false,
      false,
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
      false,
      false,
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
    const initialCalls = loadAdminActivities.mock.calls.length;
    expect(intersectionCallback).not.toBeNull();
    intersectionCallback?.(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );

    expect(loadAdminActivities).toHaveBeenCalledTimes(initialCalls);
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

  it("renders a delete button for each activity and calls handleDeleteAdminActivity on click", async () => {
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        setActivities([
          makeActivity({ id: 1, name: "Feest" }),
          makeActivity({ id: 2, name: "Borrel" }),
        ]);
        setLoading(false);
      },
    );

    renderWithProviders(<Activities />, { authService: boardAuthService });

    expect(await screen.findByText("Feest")).toBeInTheDocument();
    const deleteButtons = screen.getAllByRole("button", {
      name: "delete_activity",
    });
    expect(deleteButtons).toHaveLength(2);

    fireEvent.click(deleteButtons[0]);

    expect(handleDeleteAdminActivity).toHaveBeenCalledWith(
      1,
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("removes the deleted activity from the list when handleDeleteAdminActivity invokes onSuccess", async () => {
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        setActivities([
          makeActivity({ id: 1, name: "Feest" }),
          makeActivity({ id: 2, name: "Borrel" }),
        ]);
        setLoading(false);
      },
    );

    handleDeleteAdminActivity.mockImplementation((_id, _confirm, onSuccess) => {
      onSuccess();
    });

    renderWithProviders(<Activities />, { authService: boardAuthService });

    expect(await screen.findByText("Feest")).toBeInTheDocument();
    expect(screen.getByText("Borrel")).toBeInTheDocument();

    const deleteButtons = screen.getAllByRole("button", {
      name: "delete_activity",
    });
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.queryByText("Feest")).not.toBeInTheDocument();
    });
    expect(screen.getByText("Borrel")).toBeInTheDocument();
  });

  it("does not render delete or archive buttons for non-board users", async () => {
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        setActivities([makeActivity({ id: 1, name: "Feest" })]);
        setLoading(false);
      },
    );

    renderWithProviders(<Activities />);

    expect(await screen.findByText("Feest")).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "delete_activity" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "archive_activity" }),
    ).not.toBeInTheDocument();
  });

  it("separates published and unpublished activities into distinct sections with counts", async () => {
    loadAdminActivities.mockImplementation(
      async (_year, setLoading, setActivities) => {
        setActivities([
          makeActivity({ id: 1, name: "Published Gala", showInKoala: true }),
          makeActivity({
            id: 2,
            name: "Draft Workshop",
            showInKoala: false,
            showOnWebsite: false,
          }),
        ]);
        setLoading(false);
      },
    );

    renderWithProviders(<Activities />);

    expect(await screen.findByText("published_activities")).toBeInTheDocument();
    expect(screen.getByText("unpublished_activities")).toBeInTheDocument();

    expect(await screen.findByText("Published Gala")).toBeInTheDocument();
    expect(screen.getByText("Draft Workshop")).toBeInTheDocument();

    const publishedSection = screen
      .getByText("published_activities")
      .closest("section");
    const unpublishedSection = screen
      .getByText("unpublished_activities")
      .closest("section");

    expect(publishedSection).toHaveTextContent("Published Gala");
    expect(publishedSection).not.toHaveTextContent("Draft Workshop");

    expect(unpublishedSection).toHaveTextContent("Draft Workshop");
    expect(unpublishedSection).not.toHaveTextContent("Published Gala");
  });

  it("switches status to archived and renders archived section with unarchive button", async () => {
    loadAdminActivities.mockImplementation(
      async (
        _year,
        setLoading,
        setActivities,
        _page,
        _pageSize,
        _past,
        isArchived,
      ) => {
        if (isArchived) {
          setActivities([
            makeActivity({
              id: 99,
              name: "Old Archived Gala",
              isArchived: true,
            }),
          ]);
        } else {
          setActivities([
            makeActivity({
              id: 1,
              name: "Active Gala",
              isArchived: false,
              showInKoala: true,
            }),
          ]);
        }
        setLoading(false);
      },
    );

    renderWithProviders(<Activities />, { authService: boardAuthService });

    expect(await screen.findByText("Active Gala")).toBeInTheDocument();
    expect(screen.queryByText("Old Archived Gala")).not.toBeInTheDocument();

    const statusSelect = screen.getByLabelText("status");
    fireEvent.change(statusSelect, { target: { value: "archived" } });

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "archived_activities" }),
      ).toBeInTheDocument();
    });

    expect(await screen.findByText("Old Archived Gala")).toBeInTheDocument();
    expect(screen.queryByText("Active Gala")).not.toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "unarchive_activity" }),
    ).toBeInTheDocument();
  });
});
