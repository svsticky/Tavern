import { fireEvent, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import ActivitiesPage, { clientLoader } from "~/routes/activities/activities";
import {
  copyWeekOverview,
  downloadPosters,
  handleCreateActivityClick,
} from "~/routes/activities/activities.handlers";
import { createMockAuthService, renderWithProviders } from "~/testUtils";

const { getActivities } = vi.hoisted(() => ({ getActivities: vi.fn() }));
vi.mock("~/api", async (importOriginal) => ({
  ...(await importOriginal<typeof import("~/api")>()),
  getActivities,
}));

const { requireTokenParsed } = vi.hoisted(() => ({
  requireTokenParsed: vi.fn(),
}));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

const { useLoaderData } = vi.hoisted(() => ({ useLoaderData: vi.fn() }));
vi.mock("react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("react-router")>()),
  useLoaderData,
}));

vi.mock("~/routes/activities/activities.handlers", () => ({
  copyWeekOverview: vi.fn(),
  downloadPosters: vi.fn(),
  handleCreateActivityClick: vi.fn(),
}));

vi.mock(
  "~/components/Calendar/PersonalCalendarTile/PersonalCalendarTile",
  () => ({
    default: () => <div>personal-calendar-tile</div>,
  }),
);

vi.mock("~/components/Activity/ActivityTile/ActivityTile", () => ({
  default: ({ activity }: { activity: ActivityResponseDto }) => (
    <div>activity-tile-{activity.id}</div>
  ),
}));

function makeActivity(id: number): ActivityResponseDto {
  return { id, name: `Activity ${id}` } as ActivityResponseDto;
}

function loaderData(
  overrides: Partial<{
    activities: ActivityResponseDto[];
    isBoard: boolean;
    isInGroup: boolean;
  }> = {},
) {
  return {
    activities: [],
    isBoard: false,
    isInGroup: false,
    ...overrides,
  };
}

describe("activities clientLoader", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("waits for auth, fetches upcoming activities, and derives isBoard/isInGroup", async () => {
    requireTokenParsed.mockResolvedValue({
      is_admin: true,
      group_memberships: [],
    });
    getActivities.mockResolvedValue({ data: [makeActivity(1)] });

    const result = await clientLoader();

    expect(requireTokenParsed).toHaveBeenCalled();
    expect(getActivities).toHaveBeenCalledWith({
      query: { IncludePast: false, IncludeFuture: true },
    });
    expect(result).toEqual({
      activities: [makeActivity(1)],
      isBoard: true,
      isInGroup: true,
    });
  });

  it("is not in a group when the user has no memberships and isn't board", async () => {
    requireTokenParsed.mockResolvedValue({
      is_admin: false,
      group_memberships: [],
    });
    getActivities.mockResolvedValue({ data: [] });

    const result = await clientLoader();

    expect(result.isBoard).toBe(false);
    expect(result.isInGroup).toBe(false);
  });

  it("throws when activities fail to load", async () => {
    requireTokenParsed.mockResolvedValue({
      is_admin: false,
      group_memberships: [],
    });
    getActivities.mockResolvedValue({ error: "fail" });

    await expect(clientLoader()).rejects.toThrow("Failed to load activities");
  });
});

describe("ActivitiesPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows the no-content tile when there are no activities", () => {
    useLoaderData.mockReturnValue(loaderData());
    renderWithProviders(<ActivitiesPage />);

    expect(screen.getByText("no_upcoming_activities")).toBeInTheDocument();
  });

  it("renders an ActivityTile for each loaded activity", () => {
    useLoaderData.mockReturnValue(
      loaderData({ activities: [makeActivity(1), makeActivity(2)] }),
    );
    renderWithProviders(<ActivitiesPage />);

    expect(screen.getByText("activity-tile-1")).toBeInTheDocument();
    expect(screen.getByText("activity-tile-2")).toBeInTheDocument();
  });

  it("does not show board-only actions for a non-board user", () => {
    useLoaderData.mockReturnValue(loaderData({ isBoard: false }));
    renderWithProviders(<ActivitiesPage />);

    expect(screen.queryByText("download_posters")).not.toBeInTheDocument();
  });

  it("shows and wires up board-only actions for a board user", async () => {
    useLoaderData.mockReturnValue(loaderData({ isBoard: true }));
    const authService = createMockAuthService({
      getToken: vi.fn(async () => "tok"),
    });
    renderWithProviders(<ActivitiesPage />, { authService });

    fireEvent.click(screen.getByText("download_posters"));
    await vi.waitFor(() =>
      expect(downloadPosters).toHaveBeenCalledWith([], "tok"),
    );

    fireEvent.click(screen.getByText(/copy.*NL/));
    expect(copyWeekOverview).toHaveBeenCalledWith("NL", []);

    fireEvent.click(screen.getByText(/copy.*EN/));
    expect(copyWeekOverview).toHaveBeenCalledWith("EN", []);
  });

  it("shows a create-activity button for a group member and wires it up", () => {
    useLoaderData.mockReturnValue(loaderData({ isInGroup: true }));
    renderWithProviders(<ActivitiesPage />);

    const buttons = screen.getAllByRole("button");
    const createButton = buttons.find((b) =>
      b.querySelector("svg.lucide-plus"),
    );
    expect(createButton).toBeTruthy();
    fireEvent.click(createButton!);
    expect(handleCreateActivityClick).toHaveBeenCalled();
  });

  it("offers the personal calendar to any member, not just the board", () => {
    useLoaderData.mockReturnValue(loaderData());
    renderWithProviders(<ActivitiesPage />);

    expect(screen.getByText("personal_calendar")).toBeInTheDocument();
  });

  it("opens the personal calendar tile in a modal when the button is clicked", () => {
    useLoaderData.mockReturnValue(loaderData());
    renderWithProviders(<ActivitiesPage />);

    expect(
      screen.queryByText("personal-calendar-tile"),
    ).not.toBeInTheDocument();

    fireEvent.click(screen.getByText("personal_calendar"));

    expect(screen.getByText("personal-calendar-tile")).toBeInTheDocument();
  });
});
