import { useTranslation } from "react-i18next";
import { useLoaderData } from "react-router";
import ActivityEnrollmentOverview from "~/components/Activity/ActivityEnrollmentOverview";
import UpcomingActivities from "~/components/Activity/UpcomingActivities";
import AnnouncementsList from "~/components/Announcement/AnnouncementsList";
import DashboardHeader from "~/components/DashboardHeader";
import GroupMembershipOverview from "~/components/Group/GroupMembershipOverview";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import Button from "~/components/UI/Button";
import { requireTokenParsed } from "~/util/loaderAuth.util";
import { loadHomeLoaderData } from "./home.handlers";

/**
 * Loads the user's token and the dashboard data (see `loadHomeLoaderData`).
 * Redirects to the login page when the user isn't logged in.
 */
export async function clientLoader() {
  const tokenParsed = await requireTokenParsed();
  const data = await loadHomeLoaderData(tokenParsed.UserId);
  return { tokenParsed, ...data };
}

/**
 * Shown while the route's `clientLoader` runs on the first page load.
 *
 * @component
 */
export function HydrateFallback() {
  return <StickyLoadingLogo />;
}

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
  const {
    tokenParsed,
    activities,
    futureEnrollments,
    announcements,
    groupMemberships,
    unpaidBalances,
    enrollments,
  } = useLoaderData<typeof clientLoader>();

  return (
    <div className="flex flex-col items-center gap-5 max-w-8xl mx-auto w-full">
      {/* Dashboard Header */}
      <DashboardHeader
        name={tokenParsed.given_name}
        nextActivity={activities[0]}
        unpaidBalances={unpaidBalances}
        enrollments={enrollments}
      />

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
            <AnnouncementsList announcements={announcements} />
          </div>

          {/* Upcoming Activities */}
          <div className="flex flex-col w-full gap-y-3">
            {" "}
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
          <ActivityEnrollmentOverview enrolledActivities={futureEnrollments} />

          <p className="text-md">{t("my_groups")}</p>
          <GroupMembershipOverview groupMemberships={groupMemberships} />
        </div>
      </div>
    </div>
  );
}
