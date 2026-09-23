import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import DashboardPage, { clientLoader } from "~/routes/home/home";
import { renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const { requireTokenParsed } = vi.hoisted(() => ({
  requireTokenParsed: vi.fn(),
}));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

const { loadHomeLoaderData } = vi.hoisted(() => ({
  loadHomeLoaderData: vi.fn(),
}));
vi.mock("~/routes/home/home.handlers", () => ({ loadHomeLoaderData }));

const { useLoaderData } = vi.hoisted(() => ({ useLoaderData: vi.fn() }));
vi.mock("react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("react-router")>()),
  useLoaderData,
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

describe("home clientLoader", () => {
  it("requires a token and loads dashboard data for that user", async () => {
    requireTokenParsed.mockResolvedValue(token);
    loadHomeLoaderData.mockResolvedValue({
      activities: [{ id: 1 }],
      enrolledActivities: [],
      announcements: [],
      groupMemberships: [],
    });

    const result = await clientLoader();

    expect(loadHomeLoaderData).toHaveBeenCalledWith(token.UserId);
    expect(result).toEqual({
      tokenParsed: token,
      activities: [{ id: 1 }],
      enrolledActivities: [],
      announcements: [],
      groupMemberships: [],
    });
  });
});

describe("DashboardPage", () => {
  it("renders the dashboard content from loader data", () => {
    useLoaderData.mockReturnValue({
      tokenParsed: token,
      activities: [
        {
          id: 1,
          dateTimeEnd: new Date(Date.now() + 86_400_000).toISOString(),
        },
      ],
      enrolledActivities: [],
      announcements: [],
      groupMemberships: [],
    });

    renderWithProviders(<DashboardPage />);

    expect(screen.getByText(/dashboard-header-Jane/)).toBeInTheDocument();
    expect(screen.getByText("announcements-list")).toBeInTheDocument();
    expect(screen.getByText("upcoming-activities")).toBeInTheDocument();
    expect(screen.getByText("enrollment-overview")).toBeInTheDocument();
    expect(screen.getByText("group-membership-overview")).toBeInTheDocument();
    expect(screen.getAllByText("show_all").length).toBeGreaterThan(0);
  });
});
