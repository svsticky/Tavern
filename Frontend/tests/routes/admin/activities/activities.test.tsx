import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import * as route from "~/routes/admin/activities/activities";
import {
  fetchAdminActivities,
  handleViewActivity,
} from "~/routes/admin/activities/activities.handlers";
import { renderRoute } from "~/testUtils";

const { requireBoardOrCandidateBoard, loadCommitteeYear } = vi.hoisted(() => ({
  requireBoardOrCandidateBoard: vi.fn(),
  loadCommitteeYear: vi.fn(),
}));

vi.mock("~/util/loaderAuth.util", () => ({
  requireBoardOrCandidateBoard,
  loadCommitteeYear,
}));

vi.mock(
  "~/routes/admin/activities/activities.handlers",
  async (importOriginal) => ({
    ACTIVITIES_PAGE_SIZE: (
      await importOriginal<
        typeof import("~/routes/admin/activities/activities.handlers")
      >()
    ).ACTIVITIES_PAGE_SIZE,
    fetchAdminActivities: vi.fn(),
    handleViewActivity: vi.fn(),
  }),
);

// jsdom does not implement IntersectionObserver. Capture the callback so tests
// can simulate the end of the list coming into view.
let intersectionCallback: IntersectionObserverCallback | null = null;
class IntersectionObserverStub {
  constructor(callback: IntersectionObserverCallback) {
    intersectionCallback = callback;
  }
  observe() {}
  unobserve() {}
  disconnect() {}
}

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

function makeActivities(count: number, offset = 0) {
  return Array.from({ length: count }, (_, i) =>
    makeActivity({ id: offset + i + 1, name: `Activity ${offset + i + 1}` }),
  );
}

/** Renders the admin activities page at the given URL. */
function renderActivities(url = "/admin/activities") {
  return renderRoute(route, { path: "/admin/activities", route: url });
}

const COMMITTEE_YEAR = 2026;

describe("Activities (admin)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("IntersectionObserver", IntersectionObserverStub);
    intersectionCallback = null;
    requireBoardOrCandidateBoard.mockResolvedValue({});
    loadCommitteeYear.mockResolvedValue(COMMITTEE_YEAR);
    vi.mocked(fetchAdminActivities).mockResolvedValue([]);
  });

  it("loads the first page of the current committee year", async () => {
    renderActivities();

    await screen.findByText("no_data");
    expect(fetchAdminActivities).toHaveBeenCalledWith({
      year: COMMITTEE_YEAR,
      search: "",
      page: 1,
      pageSize: 15,
    });
  });

  it("uses the year, search and loaded pages from the URL", async () => {
    renderActivities("/admin/activities?year=2020&search=gala&pages=2");

    expect(await screen.findByLabelText("search")).toHaveValue("gala");
    expect(fetchAdminActivities).toHaveBeenCalledWith({
      year: 2020,
      search: "gala",
      page: 1,
      pageSize: 30,
    });
  });

  it.each([
    ["is in the future", "2099"],
    ["is before 2007", "2006"],
    ["is not a number", "abc"],
  ])("falls back to the committee year when the year in the URL %s", async (_, year) => {
    renderActivities(`/admin/activities?year=${year}`);

    await screen.findByText("no_data");
    expect(fetchAdminActivities).toHaveBeenCalledWith(
      expect.objectContaining({ year: COMMITTEE_YEAR }),
    );
  });

  it("shows the error page when the activities fail to load", async () => {
    vi.mocked(fetchAdminActivities).mockRejectedValue(
      new Error("Failed to load activities"),
    );
    renderActivities();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load activities",
    );
  });

  it("renders the activities with formatted price and participants", async () => {
    vi.mocked(fetchAdminActivities).mockResolvedValue([
      makeActivity({
        enrollments: [
          { isOnWaitingList: false },
          { isOnWaitingList: true },
        ] as ActivityResponseDto["enrollments"],
      }),
    ]);
    renderActivities();

    expect(await screen.findByText("Feest")).toBeInTheDocument();
    expect(screen.getByText("Kroeg")).toBeInTheDocument();
    expect(screen.getByText("€5.00")).toBeInTheDocument();
    // 1 non-waitlisted enrollment out of a limit of 50
    expect(screen.getByText(/1\/50/)).toBeInTheDocument();
  });

  it("shows 'free' for activities with no price", async () => {
    vi.mocked(fetchAdminActivities).mockResolvedValue([
      makeActivity({ price: 0 }),
    ]);
    renderActivities();

    expect(await screen.findByText("free")).toBeInTheDocument();
  });

  it("does not show a participant limit suffix when there is none", async () => {
    vi.mocked(fetchAdminActivities).mockResolvedValue([
      makeActivity({ participantLimit: null }),
    ]);
    renderActivities();

    await screen.findByText("Feest");
    expect(screen.queryByText(/\/50/)).not.toBeInTheDocument();
  });

  it("reloads the list with the search term after typing", async () => {
    renderActivities();
    await screen.findByText("no_data");
    vi.mocked(fetchAdminActivities).mockClear();

    fireEvent.change(screen.getByLabelText("search"), {
      target: { value: "gala" },
    });

    await waitFor(() =>
      expect(fetchAdminActivities).toHaveBeenCalledWith(
        expect.objectContaining({ search: "gala", page: 1 }),
      ),
    );
  });

  it("only reloads once after rapid typing", async () => {
    renderActivities();
    await screen.findByText("no_data");
    vi.mocked(fetchAdminActivities).mockClear();

    const input = screen.getByLabelText("search");
    fireEvent.change(input, { target: { value: "g" } });
    fireEvent.change(input, { target: { value: "ga" } });
    fireEvent.change(input, { target: { value: "gala" } });

    await waitFor(() => expect(fetchAdminActivities).toHaveBeenCalled());
    expect(fetchAdminActivities).toHaveBeenCalledTimes(1);
    expect(fetchAdminActivities).toHaveBeenCalledWith(
      expect.objectContaining({ search: "gala" }),
    );
  });

  it("reloads the list when the year changes", async () => {
    renderActivities();
    await screen.findByText("no_data");

    fireEvent.change(screen.getByLabelText("year"), {
      target: { value: "2024" },
    });

    await waitFor(() =>
      expect(fetchAdminActivities).toHaveBeenCalledWith(
        expect.objectContaining({ year: 2024, page: 1 }),
      ),
    );
  });

  it("keeps the search term when the year changes", async () => {
    renderActivities("/admin/activities?search=gala");
    await screen.findByText("no_data");

    fireEvent.change(screen.getByLabelText("year"), {
      target: { value: "2024" },
    });

    await waitFor(() =>
      expect(fetchAdminActivities).toHaveBeenCalledWith(
        expect.objectContaining({ year: 2024, search: "gala" }),
      ),
    );
    expect(screen.getByLabelText("search")).toHaveValue("gala");
  });

  it("calls handleViewActivity with the navigate function and activity id", async () => {
    vi.mocked(fetchAdminActivities).mockResolvedValue([
      makeActivity({ id: 7 }),
    ]);
    renderActivities();

    fireEvent.click(await screen.findByText("view_activity"));

    expect(handleViewActivity).toHaveBeenCalledWith(expect.any(Function), 7);
  });

  it("shows 'no_data' when there are no activities", async () => {
    renderActivities();

    expect(await screen.findByText("no_data")).toBeInTheDocument();
  });

  it("loads the next page when the end of the list comes into view", async () => {
    vi.mocked(fetchAdminActivities)
      .mockResolvedValueOnce(makeActivities(15))
      .mockResolvedValueOnce(makeActivities(3, 15));
    renderActivities("/admin/activities?search=gala");

    await screen.findByText("Activity 1");
    intersectionCallback!(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );

    expect(await screen.findByText("Activity 16")).toBeInTheDocument();
    expect(fetchAdminActivities).toHaveBeenLastCalledWith({
      year: COMMITTEE_YEAR,
      search: "gala",
      page: 2,
      pageSize: 15,
    });
    expect(screen.getByText("Activity 1")).toBeInTheDocument();
    expect(await screen.findByText("no_more_activities")).toBeInTheDocument();
  });

  it("does not load the next page when there are no more pages", async () => {
    vi.mocked(fetchAdminActivities).mockResolvedValue([makeActivity()]);
    renderActivities();

    await screen.findByText("Feest");
    intersectionCallback?.(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );

    expect(fetchAdminActivities).toHaveBeenCalledTimes(1);
    expect(screen.getByText("no_more_activities")).toBeInTheDocument();
  });

  it("shows a loading_more label while the next page is loading", async () => {
    vi.mocked(fetchAdminActivities)
      .mockResolvedValueOnce(makeActivities(15))
      .mockReturnValueOnce(new Promise(() => {}));
    renderActivities();

    await screen.findByText("Activity 1");
    intersectionCallback!(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );

    expect(await screen.findByText("loading_more")).toBeInTheDocument();
  });
});
