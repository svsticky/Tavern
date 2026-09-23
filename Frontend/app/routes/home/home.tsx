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
 * Resolves the current user and their dashboard data before the route
 * renders, so the page mounts already-painted instead of flashing a
 * skeleton state. Runs client-side only (Keycloak auth is client-side-only)
 * - see `HydrateFallback` below for the gap before this resolves on the
 * initial page load.
 */
export async function clientLoader() {
  const tokenParsed = await requireTokenParsed();
  const data = await loadHomeLoaderData(tokenParsed.UserId);
  return { tokenParsed, ...data };
}

/** Shown during the initial (server-rendered) page load, before `clientLoader` resolves. */
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
    enrolledActivities,
    announcements,
    groupMemberships,
  } = useLoaderData<typeof clientLoader>();

  return (
    <div className="flex flex-col items-center gap-5 max-w-8xl mx-auto w-full">
      {/* Dashboard Header */}
      <DashboardHeader
        name={tokenParsed.given_name}
        nextActivity={activities[0]}
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
            <AnnouncementsList announcements={announcements.slice(0, 2)} />
          </div>

          {/* Upcoming Activities */}
          <div className="flex flex-col w-full gap-y-3">
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
            enrolledActivities={enrolledActivities.filter(
              (a) => new Date(a.dateTimeEnd) >= new Date(Date.now()),
            )}
          />

          <p className="text-md">{t("my_groups")}</p>
          <GroupMembershipOverview groupMemberships={groupMemberships} />
        </div>
      </div>
    </div>
  );
}
