import { beforeEach, describe, expect, it, vi } from "vitest";
import * as route from "~/routes/home/home";
import { loadHomeLoaderData } from "~/routes/home/home.handlers";
import { renderRoute, screen } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const { requireTokenParsed } = vi.hoisted(() => ({
  requireTokenParsed: vi.fn(),
}));

vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

vi.mock("~/routes/home/home.handlers", () => ({
  loadHomeLoaderData: vi.fn(),
}));

vi.mock("~/components/DashboardHeader", () => ({
  default: ({
    name,
    nextActivity,
  }: {
    name: string;
    nextActivity?: { name: string };
  }) => (
    <div>
      dashboard-header-{name}-{nextActivity?.name ?? "none"}
    </div>
  ),
}));

vi.mock("~/components/Activity/UpcomingActivities", () => ({
  default: ({ activities }: { activities: unknown[] }) => (
    <div>upcoming-activities-{activities.length}</div>
  ),
}));

vi.mock("~/components/Activity/ActivityEnrollmentOverview", () => ({
  default: ({ enrolledActivities }: { enrolledActivities: unknown[] }) => (
    <div>enrollment-overview-{enrolledActivities.length}</div>
  ),
}));

vi.mock("~/components/Announcement/AnnouncementsList", () => ({
  default: ({ announcements }: { announcements: unknown[] }) => (
    <div>announcements-list-{announcements.length}</div>
  ),
}));

vi.mock("~/components/Group/GroupMembershipOverview", () => ({
  default: ({ groupMemberships }: { groupMemberships: unknown[] }) => (
    <div>group-membership-overview-{groupMemberships.length}</div>
  ),
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
    requireTokenParsed.mockResolvedValue(token);
    vi.mocked(loadHomeLoaderData).mockResolvedValue({
      activities: [{ name: "Borrel" }, { name: "Lezing" }],
      futureEnrollments: [{}],
      announcements: [{}, {}],
      groupMemberships: [{}, {}, {}],
      unpaidBalances: [],
      enrollments: [],
    } as never);
  });

  it("loads the dashboard data for the logged-in user", async () => {
    renderRoute(route);

    await screen.findByText("dashboard-header-Jane-Borrel");
    expect(loadHomeLoaderData).toHaveBeenCalledWith(token.UserId);
  });

  it("renders the dashboard sections with the loaded data", async () => {
    renderRoute(route);

    expect(await screen.findByText("announcements-list-2")).toBeInTheDocument();
    expect(screen.getByText("upcoming-activities-2")).toBeInTheDocument();
    expect(screen.getByText("enrollment-overview-1")).toBeInTheDocument();
    expect(screen.getByText("group-membership-overview-3")).toBeInTheDocument();
    expect(screen.getAllByText("show_all")).toHaveLength(2);
  });

  it("shows the error page when the data fails to load", async () => {
    vi.mocked(loadHomeLoaderData).mockRejectedValue(
      new Error("Failed to load activities"),
    );
    renderRoute(route);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load activities",
    );
  });
});
