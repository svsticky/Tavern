import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import DashboardPage from "~/routes/home/home";
import { loadHomePageData } from "~/routes/home/home.handlers";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

vi.mock("~/routes/home/home.handlers", () => ({
  loadHomePageData: vi.fn(),
}));

vi.mock("~/components/DashboardHeader", () => ({
  default: ({ name }: { name: string }) => <div>dashboard-header-{name}</div>,
}));

vi.mock("~/components/Activity/UpcomingActivities", () => ({
  default: () => <div>upcoming-activities</div>,
}));

vi.mock("~/components/Activity/ActivityEnrollmentOverview", () => ({
  default: () => <div>enrollment-overview</div>,
}));

vi.mock("~/components/Announcement/AnnouncementsList", () => ({
  default: () => <div>announcements-list</div>,
}));

vi.mock("~/components/Group/GroupMembershipOverview", () => ({
  default: () => <div>group-membership-overview</div>,
}));

const token: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Jane",
  family_name: "Doe",
  name: "Jane Doe",
};

describe("DashboardPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders nothing while the token has not loaded", () => {
    const authService = createMockAuthService({
      isReady: vi.fn(() => true),
      getTokenParsed: vi.fn(() => new Promise<TokenParsed | null>(() => {})),
    });
    const { container } = renderWithProviders(<DashboardPage />, {
      authService,
    });
    expect(container).toBeEmptyDOMElement();
  });

  it("does not load token/auth when the auth service is not ready", async () => {
    const authService = createMockAuthService({
      isReady: vi.fn(() => false),
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardPage />, { authService });

    await new Promise((r) => setTimeout(r, 0));
    expect(authService.getTokenParsed).not.toHaveBeenCalled();
  });

  it("shows a loading indicator once the token is available and data is loading", async () => {
    const authService = createMockAuthService({
      isReady: vi.fn(() => true),
      getTokenParsed: vi.fn(async () => token),
      isAuthenticated: vi.fn(() => true),
    });
    renderWithProviders(<DashboardPage />, { authService });

    expect(
      await screen.findByText(/dashboard-header-Jane/),
    ).toBeInTheDocument();
    expect(screen.getByText(/loading_dashboard/)).toBeInTheDocument();
    await waitFor(() => expect(loadHomePageData).toHaveBeenCalled());
  });

  it("does not call loadHomePageData when the user is not authenticated", async () => {
    const authService = createMockAuthService({
      isReady: vi.fn(() => true),
      getTokenParsed: vi.fn(async () => token),
      isAuthenticated: vi.fn(() => false),
    });
    renderWithProviders(<DashboardPage />, { authService });

    await screen.findByText(/dashboard-header-Jane/);
    await new Promise((r) => setTimeout(r, 0));
    expect(loadHomePageData).not.toHaveBeenCalled();
  });

  it("renders the dashboard content once loading completes", async () => {
    vi.mocked(loadHomePageData).mockImplementation(async ({ setLoading }) => {
      setLoading(false);
    });
    const authService = createMockAuthService({
      isReady: vi.fn(() => true),
      getTokenParsed: vi.fn(async () => token),
      isAuthenticated: vi.fn(() => true),
    });
    renderWithProviders(<DashboardPage />, { authService });

    await waitFor(() =>
      expect(screen.getByText("announcements-list")).toBeInTheDocument(),
    );
    expect(screen.getByText("upcoming-activities")).toBeInTheDocument();
    expect(screen.getByText("enrollment-overview")).toBeInTheDocument();
    expect(screen.getByText("group-membership-overview")).toBeInTheDocument();
    expect(screen.getAllByText("show_all").length).toBeGreaterThan(0);
    expect(
      screen.getByRole("button", { name: /personalise_dashboard/i }),
    ).toBeInTheDocument();
  });

  it("opens the personalise dashboard modal when clicking customise button", async () => {
    vi.mocked(loadHomePageData).mockImplementation(async ({ setLoading }) => {
      setLoading(false);
    });
    const authService = createMockAuthService({
      isReady: vi.fn(() => true),
      getTokenParsed: vi.fn(async () => token),
      isAuthenticated: vi.fn(() => true),
    });
    renderWithProviders(<DashboardPage />, { authService });

    const personaliseBtn = await screen.findByRole("button", {
      name: /personalise_dashboard/i,
    });
    fireEvent.click(personaliseBtn);

    expect(await screen.findByText("main_content")).toBeInTheDocument();
  });

  it("renders empty state when all widgets are disabled and allows reopening modal", async () => {
    localStorage.setItem(
      `tavern_dashboard_widgets_${token.UserId}`,
      JSON.stringify([
        { id: "announcements", visible: false, column: "main", order: 0 },
        { id: "upcoming_activities", visible: false, column: "main", order: 1 },
        { id: "my_enrollments", visible: false, column: "sidebar", order: 0 },
        { id: "my_groups", visible: false, column: "sidebar", order: 1 },
      ]),
    );

    vi.mocked(loadHomePageData).mockImplementation(async ({ setLoading }) => {
      setLoading(false);
    });
    const authService = createMockAuthService({
      isReady: vi.fn(() => true),
      getTokenParsed: vi.fn(async () => token),
      isAuthenticated: vi.fn(() => true),
    });
    renderWithProviders(<DashboardPage />, { authService });

    expect(await screen.findByText("no_widgets_enabled")).toBeInTheDocument();

    // Click the personalise button inside the empty state
    const emptyStateButtons = screen.getAllByRole("button", {
      name: /personalise_dashboard/i,
    });
    fireEvent.click(emptyStateButtons[emptyStateButtons.length - 1]);
    expect(await screen.findByText("main_content")).toBeInTheDocument();

    // Click reset to default from modal
    const resetBtn = screen.getByRole("button", { name: /reset_to_default/i });
    fireEvent.click(resetBtn);

    // Modal closes and widgets re-appear
    await waitFor(() => {
      expect(screen.getByText("announcements-list")).toBeInTheDocument();
    });
  });

  it("handles saving customized widgets from the modal", async () => {
    vi.mocked(loadHomePageData).mockImplementation(async ({ setLoading }) => {
      setLoading(false);
    });
    const authService = createMockAuthService({
      isReady: vi.fn(() => true),
      getTokenParsed: vi.fn(async () => token),
      isAuthenticated: vi.fn(() => true),
    });
    renderWithProviders(<DashboardPage />, { authService });

    const personaliseBtn = await screen.findByRole("button", {
      name: /personalise_dashboard/i,
    });
    fireEvent.click(personaliseBtn);
    expect(await screen.findByText("main_content")).toBeInTheDocument();

    // Click done (save)
    const doneBtn = screen.getByRole("button", { name: /done/i });
    fireEvent.click(doneBtn);

    await waitFor(() => {
      expect(screen.queryByText("main_content")).not.toBeInTheDocument();
    });
  });

  it("filters out past enrolled activities and renders future ones", async () => {
    vi.mocked(loadHomePageData).mockImplementation(
      async ({ setLoading, setEnrolledActivities }) => {
        setEnrolledActivities([
          {
            id: 1,
            name: "Past Activity",
            dateTimeStart: new Date(Date.now() - 7200000).toISOString(),
            dateTimeEnd: new Date(Date.now() - 3600000).toISOString(),
          } as any,
          {
            id: 2,
            name: "Future Activity",
            dateTimeStart: new Date(Date.now() + 3600000).toISOString(),
            dateTimeEnd: new Date(Date.now() + 7200000).toISOString(),
          } as any,
        ]);
        setLoading(false);
      },
    );
    const authService = createMockAuthService({
      isReady: vi.fn(() => true),
      getTokenParsed: vi.fn(async () => token),
      isAuthenticated: vi.fn(() => true),
    });
    renderWithProviders(<DashboardPage />, { authService });

    await waitFor(() =>
      expect(screen.getByText("enrollment-overview")).toBeInTheDocument(),
    );
  });

  it("renders correctly when only sidebar widgets are visible", async () => {
    localStorage.setItem(
      `tavern_dashboard_widgets_${token.UserId}`,
      JSON.stringify([
        { id: "announcements", visible: false, column: "main", order: 0 },
        { id: "upcoming_activities", visible: false, column: "main", order: 1 },
        { id: "my_enrollments", visible: true, column: "sidebar", order: 0 },
        { id: "my_groups", visible: true, column: "sidebar", order: 1 },
      ]),
    );

    vi.mocked(loadHomePageData).mockImplementation(async ({ setLoading }) => {
      setLoading(false);
    });
    const authService = createMockAuthService({
      isReady: vi.fn(() => true),
      getTokenParsed: vi.fn(async () => token),
      isAuthenticated: vi.fn(() => true),
    });
    renderWithProviders(<DashboardPage />, { authService });

    await waitFor(() =>
      expect(screen.getByText("enrollment-overview")).toBeInTheDocument(),
    );
    expect(screen.queryByText("announcements-list")).not.toBeInTheDocument();
  });
});
