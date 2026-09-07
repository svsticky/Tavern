import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import ActivitiesPage from "~/routes/activities/activities";
import {
  copyWeekOverview,
  downloadPosters,
  handleCreateActivityClick,
  loadActivities,
} from "~/routes/activities/activities.handlers";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

vi.mock("~/routes/activities/activities.handlers", () => ({
  loadActivities: vi.fn(),
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

function makeActivity(id: number): ActivityResponseDto {
  return { id, name: `Activity ${id}` } as ActivityResponseDto;
}

describe("ActivitiesPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it("renders nothing while the token has not loaded", () => {
    const authService = createMockAuthService({
      getToken: vi.fn(() => new Promise<string | null>(() => {})),
      getTokenParsed: vi.fn(() => new Promise<TokenParsed | null>(() => {})),
    });
    const { container } = renderWithProviders(<ActivitiesPage />, {
      authService,
    });
    expect(container).toBeEmptyDOMElement();
  });

  it("logs an error when there is no parsed token", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getToken: vi.fn(async () => null),
      getTokenParsed: vi.fn(async () => null),
    });
    renderWithProviders(<ActivitiesPage />, { authService });

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("calls loadActivities once the token is loaded", async () => {
    const authService = createMockAuthService({
      getToken: vi.fn(async () => "tok"),
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(<ActivitiesPage />, { authService });

    await waitFor(() => expect(loadActivities).toHaveBeenCalled());
  });

  it("shows the no-content tile when there are no activities", async () => {
    vi.mocked(loadActivities).mockImplementation(async ({ setLoading }) => {
      setLoading(false);
    });
    const authService = createMockAuthService({
      getToken: vi.fn(async () => "tok"),
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(<ActivitiesPage />, { authService });

    expect(
      await screen.findByText("no_upcoming_activities"),
    ).toBeInTheDocument();
  });

  it("renders an ActivityTile for each loaded activity", async () => {
    vi.mocked(loadActivities).mockImplementation(
      async ({ setLoading, setActivities }) => {
        setActivities([makeActivity(1), makeActivity(2)]);
        setLoading(false);
      },
    );
    const authService = createMockAuthService({
      getToken: vi.fn(async () => "tok"),
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(<ActivitiesPage />, { authService });

    expect(await screen.findByText("activity-tile-1")).toBeInTheDocument();
    expect(screen.getByText("activity-tile-2")).toBeInTheDocument();
  });

  it("does not show board-only actions for a non-board user", async () => {
    const authService = createMockAuthService({
      getToken: vi.fn(async () => "tok"),
      getTokenParsed: vi.fn(async () => ({
        ...memberToken,
        is_admin: false,
      })),
    });
    renderWithProviders(<ActivitiesPage />, { authService });

    await waitFor(() => expect(loadActivities).toHaveBeenCalled());
    expect(screen.queryByText("download_posters")).not.toBeInTheDocument();
  });

  it("shows and wires up board-only actions for a board user", async () => {
    const authService = createMockAuthService({
      getToken: vi.fn(async () => "tok"),
      getTokenParsed: vi.fn(async () => ({
        ...memberToken,
        is_admin: true,
      })),
    });
    renderWithProviders(<ActivitiesPage />, { authService });

    const menuButton = await screen.findByLabelText("Board Actions");
    expect(menuButton).toBeInTheDocument();

    // Initially closed
    expect(screen.queryByText("download_posters")).not.toBeInTheDocument();

    // Open dropdown
    fireEvent.click(menuButton);
    const downloadButton = screen.getByText("download_posters");
    fireEvent.click(downloadButton);
    expect(downloadPosters).toHaveBeenCalledWith([], "tok");

    // Open dropdown again for copy NL
    fireEvent.click(menuButton);
    fireEvent.click(screen.getByText(/copy.*NL/));
    expect(copyWeekOverview).toHaveBeenCalledWith("NL", []);

    // Open dropdown again for copy EN
    fireEvent.click(menuButton);
    fireEvent.click(screen.getByText(/copy.*EN/));
    expect(copyWeekOverview).toHaveBeenCalledWith("EN", []);

    // Verify outside click closes dropdown
    fireEvent.click(menuButton);
    expect(screen.getByText("download_posters")).toBeInTheDocument();
    fireEvent.mouseDown(document.body);
    expect(screen.queryByText("download_posters")).not.toBeInTheDocument();
  });

  it("shows a create-activity button for a group member and wires it up", async () => {
    const authService = createMockAuthService({
      getToken: vi.fn(async () => "tok"),
      getTokenParsed: vi.fn(async () => ({
        ...memberToken,
        is_admin: true,
      })),
    });
    renderWithProviders(<ActivitiesPage />, { authService });

    await waitFor(() => expect(loadActivities).toHaveBeenCalled());
    const buttons = screen.getAllByRole("button");
    const createButton = buttons.find((b) =>
      b.querySelector("svg.lucide-plus"),
    );
    expect(createButton).toBeTruthy();
    fireEvent.click(createButton!);
    expect(handleCreateActivityClick).toHaveBeenCalled();
  });
  it("offers the personal calendar to any member, not just the board", async () => {
    const authService = createMockAuthService({
      getToken: vi.fn(async () => "tok"),
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(<ActivitiesPage />, { authService });

    expect(await screen.findByText("personal_calendar")).toBeInTheDocument();
  });

  it("opens the personal calendar tile in a modal when the button is clicked", async () => {
    const authService = createMockAuthService({
      getToken: vi.fn(async () => "tok"),
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(<ActivitiesPage />, { authService });

    expect(
      screen.queryByText("personal-calendar-tile"),
    ).not.toBeInTheDocument();

    fireEvent.click(await screen.findByText("personal_calendar"));

    expect(
      await screen.findByText("personal-calendar-tile"),
    ).toBeInTheDocument();
  });

  it("renders filter tabs and allows switching to enrolled history", async () => {
    const authService = createMockAuthService({
      getToken: vi.fn(async () => "tok"),
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(<ActivitiesPage />, { authService });

    await waitFor(() => expect(loadActivities).toHaveBeenCalled());

    const historyBtn = await screen.findByRole("button", {
      name: /enrolled_history|enrolled history/i,
    });
    expect(historyBtn).toBeInTheDocument();

    fireEvent.click(historyBtn);

    await waitFor(() =>
      expect(loadActivities).toHaveBeenCalledWith(
        expect.objectContaining({
          filter: "history",
          userId: memberToken.UserId,
        }),
      ),
    );
  });

  it("filters activities based on the search query", async () => {
    vi.mocked(loadActivities).mockImplementation(
      async ({ setLoading, setActivities }) => {
        setActivities([
          { id: 1, name: "Pizza Night", location: "Sticky Room" } as any,
          { id: 2, name: "Lan Party", location: "Main Hall" } as any,
        ]);
        setLoading(false);
      },
    );
    const authService = createMockAuthService({
      getToken: vi.fn(async () => "tok"),
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(<ActivitiesPage />, { authService });

    expect(await screen.findByText("activity-tile-1")).toBeInTheDocument();
    expect(screen.getByText("activity-tile-2")).toBeInTheDocument();

    const searchInput = screen.getByRole("textbox", {
      name: /search_activities/i,
    });
    fireEvent.change(searchInput, { target: { value: "pizza" } });

    expect(screen.getByText("activity-tile-1")).toBeInTheDocument();
    expect(screen.queryByText("activity-tile-2")).not.toBeInTheDocument();

    fireEvent.change(searchInput, { target: { value: "nonexistent" } });
    expect(screen.queryByText("activity-tile-1")).not.toBeInTheDocument();
    expect(screen.getByText("no_activities_found")).toBeInTheDocument();
  });
});
