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
import { getCommitteeYear } from "~/util/date.util";

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
    const inKoala = { ...makeActivity(1), showInKoala: true };
    const onWebsite = { ...makeActivity(2), showOnWebsite: true };
    const hidden = makeActivity(3);
    const activities = [inKoala, onWebsite, hidden];
    getActivities.mockResolvedValue({ data: activities });
    const authService = createMockAuthService({
      getToken: vi.fn(async () => "tok"),
    });
    renderRoute(route, { authService });

    fireEvent.click(await screen.findByText("download_posters"));
    await vi.waitFor(() =>
      expect(downloadPosters).toHaveBeenCalledWith(activities, "tok"),
    );

    // The week overview only promotes activities that are shown in Koala or on the website.
    fireEvent.click(screen.getByText(/copy.*NL/));
    expect(copyWeekOverview).toHaveBeenCalledWith("NL", [inKoala, onWebsite]);

    fireEvent.click(screen.getByText(/copy.*EN/));
    expect(copyWeekOverview).toHaveBeenCalledWith("EN", [inKoala, onWebsite]);
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
  it("toggles the board dropdown and closes it on Escape or an outside click", async () => {
    requireTokenParsed.mockResolvedValue(boardToken);
    renderRoute(route);

    await screen.findByText("download_posters");
    const toggle = screen
      .getAllByRole("button")
      .find((b) => b.querySelector("svg.lucide-menu"))!;
    expect(toggle).toHaveAttribute("aria-expanded", "false");

    fireEvent.click(toggle);
    expect(toggle).toHaveAttribute("aria-expanded", "true");

    // Clicks inside the dropdown keep it open
    fireEvent.mouseDown(screen.getByText("download_posters"));
    expect(toggle).toHaveAttribute("aria-expanded", "true");

    fireEvent.keyDown(document, { key: "Enter" });
    expect(toggle).toHaveAttribute("aria-expanded", "true");

    fireEvent.keyDown(document, { key: "Escape" });
    expect(toggle).toHaveAttribute("aria-expanded", "false");

    fireEvent.click(toggle);
    fireEvent.mouseDown(document.body);
    expect(toggle).toHaveAttribute("aria-expanded", "false");
  });

  it("closes the personal calendar modal", async () => {
    renderRoute(route);

    fireEvent.click(await screen.findByText("personal_calendar"));
    expect(
      await screen.findByText("personal-calendar-tile"),
    ).toBeInTheDocument();

    fireEvent.click(screen.getByLabelText("close_modal"));
    await vi.waitFor(() =>
      expect(
        screen.queryByText("personal-calendar-tile"),
      ).not.toBeInTheDocument(),
    );
  });

  it("shows the create-activity button only for members of a current-year group", async () => {
    const year = getCommitteeYear();
    requireTokenParsed.mockResolvedValue({
      ...memberToken,
      group_memberships: [`${year - 1}:old-committee`, `${year}:committee`],
    });
    renderRoute(route);

    await screen.findByText("personal_calendar");
    const createButton = screen
      .getAllByRole("button")
      .find((b) => b.querySelector("svg.lucide-plus"));
    expect(createButton).toBeTruthy();
    expect(screen.queryByText("download_posters")).not.toBeInTheDocument();
  });

  it("hides the create-activity button for members of only past groups", async () => {
    const year = getCommitteeYear();
    requireTokenParsed.mockResolvedValue({
      ...memberToken,
      group_memberships: [`${year - 1}:old-committee`],
    });
    renderRoute(route);

    await screen.findByText("personal_calendar");
    const createButton = screen
      .getAllByRole("button")
      .find((b) => b.querySelector("svg.lucide-plus"));
    expect(createButton).toBeUndefined();
  });
});
