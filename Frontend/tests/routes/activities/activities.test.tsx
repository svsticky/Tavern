import { fireEvent, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import * as route from "~/routes/activities/activities";
import {
  copyWeekOverview,
  downloadPosters,
  handleCreateActivityClick,
} from "~/routes/activities/activities.handlers";
import { createMockAuthService, renderRoute } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const { getActivities, requireTokenParsed } = vi.hoisted(() => ({
  getActivities: vi.fn(),
  requireTokenParsed: vi.fn(),
}));

vi.mock("~/api", () => ({ getActivities }));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

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

const memberToken: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Test",
  family_name: "User",
  name: "Test User",
};

const boardToken = { ...memberToken, is_admin: true } as TokenParsed;

function makeActivity(id: number): ActivityResponseDto {
  return { id, name: `Activity ${id}` } as ActivityResponseDto;
}

describe("ActivitiesPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireTokenParsed.mockResolvedValue(memberToken);
    getActivities.mockResolvedValue({ data: [] });
  });

  it("shows the no-content tile when there are no activities", async () => {
    renderRoute(route);

    expect(
      await screen.findByText("no_upcoming_activities"),
    ).toBeInTheDocument();
  });

  it("renders an ActivityTile for each loaded activity", async () => {
    getActivities.mockResolvedValue({
      data: [makeActivity(1), makeActivity(2)],
    });
    renderRoute(route);

    expect(await screen.findByText("activity-tile-1")).toBeInTheDocument();
    expect(screen.getByText("activity-tile-2")).toBeInTheDocument();
  });

  it("shows the error page when the activities fail to load", async () => {
    getActivities.mockResolvedValue({ error: "fail" });
    renderRoute(route);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load activities",
    );
  });

  it("does not show board-only actions for a non-board user", async () => {
    renderRoute(route);

    await screen.findByText("personal_calendar");
    expect(screen.queryByText("download_posters")).not.toBeInTheDocument();
  });

  it("shows and wires up board-only actions for a board user", async () => {
    requireTokenParsed.mockResolvedValue(boardToken);
    const activities = [makeActivity(1)];
    getActivities.mockResolvedValue({ data: activities });
    const authService = createMockAuthService({
      getToken: vi.fn(async () => "tok"),
    });
    renderRoute(route, { authService });

    fireEvent.click(await screen.findByText("download_posters"));
    await vi.waitFor(() =>
      expect(downloadPosters).toHaveBeenCalledWith(activities, "tok"),
    );

    fireEvent.click(screen.getByText(/copy.*NL/));
    expect(copyWeekOverview).toHaveBeenCalledWith("NL", activities);

    fireEvent.click(screen.getByText(/copy.*EN/));
    expect(copyWeekOverview).toHaveBeenCalledWith("EN", activities);
  });

  it("shows a create-activity button for a board member and wires it up", async () => {
    requireTokenParsed.mockResolvedValue(boardToken);
    renderRoute(route);

    await screen.findByText("download_posters");
    const createButton = screen
      .getAllByRole("button")
      .find((b) => b.querySelector("svg.lucide-plus"));
    expect(createButton).toBeTruthy();
    fireEvent.click(createButton!);
    expect(handleCreateActivityClick).toHaveBeenCalled();
  });

  it("does not show the create-activity button for a member outside any group", async () => {
    renderRoute(route);

    await screen.findByText("personal_calendar");
    expect(document.querySelector("svg.lucide-plus")).toBeNull();
  });

  it("offers the personal calendar to any member, not just the board", async () => {
    renderRoute(route);

    expect(await screen.findByText("personal_calendar")).toBeInTheDocument();
  });

  it("opens the personal calendar tile in a modal when the button is clicked", async () => {
    renderRoute(route);

    fireEvent.click(await screen.findByText("personal_calendar"));

    expect(
      await screen.findByText("personal-calendar-tile"),
    ).toBeInTheDocument();
  });
});
