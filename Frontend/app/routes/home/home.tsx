import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import type {
  ActivityResponseDto,
  GetAnnouncementResponseDto,
  GroupMembershipResponseDto,
} from "~/api";
import ActivityEnrollmentOverview from "~/components/Activity/ActivityEnrollmentOverview";
import UpcomingActivities from "~/components/Activity/UpcomingActivities";
import AnnouncementsList from "~/components/Announcement/AnnouncementsList";
import DashboardHeader from "~/components/DashboardHeader";
import GroupMembershipOverview from "~/components/Group/GroupMembershipOverview";
import Button from "~/components/UI/Button";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { loadHomePageData } from "./home.handlers";

/**
 * The main application landing page for authenticated members.
 *
 * This component acts as a high-level summary of the user's association life. It aggregates:
 * - **Personalized Greeting**: Welcomes the user and highlights their next upcoming activity.
 * - **Latest Announcements**: Shows a preview of the most recent association-wide updates.
 * - **Activity Feed**: Displays a comprehensive list of upcoming events.
 * - **Personal Overview**: A sidebar containing the user's current activity enrollments and
 *   their committee/group memberships.
 *
 * Features:
 * - **Contextual Loading**: Displays a skeleton-style loading state while coordinating multiple API requests.
 * - **Auth Integration**: Deeply integrates with auth service to filter data based on the user's `UserId`.
 * - **Responsive Layout**: Uses a grid system that transitions from a single-column mobile view to a
 *   split main/sidebar layout on larger screens.
 *
 * @page
 * @component
 */
export default function DashboardPage() {
  const { t } = useTranslation();
  const authService = useAuth();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);
  const [authenticated, setAuthenticated] = useState<boolean | null>(null);

  useEffect(() => {
    const loadTokenAndAuth = async () => {
      if (!authService.isReady()) return;

      const tokenParsed = await authService.getTokenParsed();
      const authenticated = await authService.isAuthenticated();
      setTokenParsed(tokenParsed);
      setAuthenticated(authenticated);
    };
    loadTokenAndAuth();
  }, [authService]);

  const [activities, setActivities] = useState<ActivityResponseDto[]>([]);
  const [enrolledActivities, setEnrolledActivities] = useState<
    ActivityResponseDto[]
  >([]);
  const [announcements, setAnnouncements] = useState<
    GetAnnouncementResponseDto[]
  >([]);
  const [groupMemberships, setGroupMemberships] = useState<
    GroupMembershipResponseDto[]
  >([]);

  const [loading, setLoading] = useState(true);
  useEffect(() => {
    if (authenticated === null || tokenParsed === null) {
      return;
    }

    if (!authenticated) return;

    loadHomePageData({
      authenticated: authenticated,
      userId: tokenParsed.UserId,
      setLoading,
      setActivities,
      setAnnouncements,
      setGroupMemberships,
      setEnrolledActivities,
    });
  }, [authenticated, tokenParsed]);

  if (!tokenParsed) {
    return null;
  }

  return (
    <div className="flex flex-col items-center gap-5 max-w-8xl mx-auto w-full">
      {/* Dashboard Header */}
      <DashboardHeader
        name={tokenParsed.given_name}
        nextActivity={activities[0]}
      />

      {loading ? (
        <div className="flex flex-col items-center justify-center min-h-[400px] w-full gap-4">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-(--board-primary-light)"></div>
          <p className="text-gray-500 animate-pulse">
            {t("loading_dashboard")}...
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-4 w-full gap-5 animate-in fade-in duration-500">
          <div className="flex flex-col w-full gap-y-8 col-span-4 lg:col-span-3">
            {/* Announcements */}
            <div className="flex flex-col w-full gap-y-3">
              <div className="flex w-full justify-between items-center">
                <p className="font-semibold text-lg">
                  {t("latest_announcements")}
                </p>
                <Button
                  showArrow
                  className="bg-transparent p-0 hover:bg-transparent text-(--board-primary) hover:text-(--board-primary-light) shadow-none"
                  href="/announcements"
                >
                  {t("show_all")}
                </Button>
              </div>
              <AnnouncementsList announcements={announcements.slice(0, 2)} />
            </div>

            {/* Upcoming Activities */}
            <div className="flex flex-col w-full gap-y-3">
              {" "}
              {/* Toegevoegd: flex flex-col en gap-y-3 */}
              <div className="flex w-full justify-between items-center">
                <p className="font-semibold text-lg">
                  {t("upcoming_activities")}
                </p>
                <Button
                  showArrow
                  className="bg-transparent p-0 hover:bg-transparent text-(--board-primary) hover:text-(--board-primary-light) shadow-none"
                  href="/activities"
                >
                  {t("show_all")}
                </Button>
              </div>
              <UpcomingActivities activities={activities} />
            </div>
          </div>

          {/* Enrollments and Committees */}
          <div className="flex flex-col col-span-4 lg:col-span-1 gap-3">
            <p className="text-md">{t("my_enrollments")}</p>
            <ActivityEnrollmentOverview
              enrolledActivities={enrolledActivities}
            />

            <p className="text-md">{t("my_groups")}</p>
            <GroupMembershipOverview groupMemberships={groupMemberships} />
          </div>
        </div>
      )}
    </div>
  );
}
