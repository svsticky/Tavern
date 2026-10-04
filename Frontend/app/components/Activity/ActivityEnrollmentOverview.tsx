import { t } from "i18next";
import { CircleCheckBig } from "lucide-react";
import { useState } from "react";
import { Link } from "react-router";
import type { ActivityResponseDto } from "~/api";
import { formatDate } from "~/util/date.util";
import { cn } from "~/util/tailwind.util";
import { ListTile } from "../Tiles/ListTile";
import { NoContentTile } from "../Tiles/NoContentTile";
import Tile from "../Tiles/Tile";

/**
 * A specialized list component that displays a collection of activities
 * the current user is enrolled in, categorized into upcoming and past events.
 *
 * Features:
 * - **Tabbed Navigation**: Switch easily between upcoming and past enrolled activities.
 * - **Empty State Handling**: Automatically renders a `NoContentTile` with a
 *   localized message if the enrollment list is empty.
 * - **Visual Indicators**: Each activity is represented with a themed checkmark icon
 *   using color-mixing to match the organization's primary brand color.
 * - **Navigation**: Each item acts as a `Link` to the detailed activity page,
 *   complete with hover states for better interactivity.
 * - **Date Formatting**: Displays the start date using a standardized "shortDate"
 *   utility for consistent UI presentation.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {ActivityResponseDto[]} props.enrolledActivities - An array of activity objects
 * representing the user's current and past enrollments.
 *
 * @example
 * ```tsx
 * <ActivityEnrollmentOverview
 *   enrolledActivities={userEnrollments}
 * />
 * ```
 */
export default function ActivityEnrollmentOverview({
  enrolledActivities,
}: {
  enrolledActivities: ActivityResponseDto[];
}) {
  const now = new Date();

  const upcomingActivities = enrolledActivities
    .filter((a) => new Date(a.dateTimeEnd || a.dateTimeStart) >= now)
    .sort(
      (a, b) =>
        new Date(a.dateTimeStart).getTime() -
        new Date(b.dateTimeStart).getTime(),
    );

  const pastActivities = enrolledActivities
    .filter((a) => new Date(a.dateTimeEnd || a.dateTimeStart) < now)
    .sort(
      (a, b) =>
        new Date(b.dateTimeStart).getTime() -
        new Date(a.dateTimeStart).getTime(),
    );

  const [activeTab, setActiveTab] = useState<"upcoming" | "past">(() => {
    if (upcomingActivities.length === 0 && pastActivities.length > 0) {
      return "past";
    }
    return "upcoming";
  });

  if (enrolledActivities.length === 0) {
    return <NoContentTile text={t("no_enrollments")} />;
  }

  const displayedActivities =
    activeTab === "upcoming" ? upcomingActivities : pastActivities;

  return (
    <div className="flex flex-col gap-2 w-full">
      <div className="flex items-center gap-1 p-1 bg-gray-100 rounded-xl">
        <button
          type="button"
          onClick={() => setActiveTab("upcoming")}
          className={cn(
            "flex-1 py-1 px-2 text-xs font-semibold rounded-lg transition-all text-center cursor-pointer",
            activeTab === "upcoming"
              ? "bg-white text-slate-900 shadow-sm"
              : "text-slate-600 hover:text-slate-900",
          )}
        >
          {t("upcoming")} ({upcomingActivities.length})
        </button>
        <button
          type="button"
          onClick={() => setActiveTab("past")}
          className={cn(
            "flex-1 py-1 px-2 text-xs font-semibold rounded-lg transition-all text-center cursor-pointer",
            activeTab === "past"
              ? "bg-white text-slate-900 shadow-sm"
              : "text-slate-600 hover:text-slate-900",
          )}
        >
          {t("past")} ({pastActivities.length})
        </button>
      </div>

      {displayedActivities.length === 0 ? (
        <NoContentTile
          text={
            activeTab === "upcoming"
              ? t("no_enrollments")
              : t("no_past_enrollments")
          }
        />
      ) : (
        <ListTile className="w-full">
          {displayedActivities.map((activity) => (
            <Tile key={activity.id} className="p-0">
              <Link
                key={activity.id}
                className="!text-black mt-0 flex p-2 gap-2 hover:bg-gray-50 mt-0"
                to={`/activities/${activity.id}`}
              >
                {/* Icon Container */}
                <div className="bg-[color-mix(in_srgb,var(--board-primary),white_80%)] rounded-xl w-10 h-10 flex items-center justify-center shrink-0">
                  <CircleCheckBig className="text-(--board-primary) h-full" />
                </div>

                {/* Activity Details */}
                <div className="min-w-0">
                  <p className="truncate mt-[-2.5px] font-medium leading-tight">
                    {activity.name}
                  </p>
                  <p className="text-gray-500 text-xs mt-0.5">
                    {formatDate(new Date(activity.dateTimeStart), "shortDate")}
                  </p>
                </div>
              </Link>
            </Tile>
          ))}
        </ListTile>
      )}
    </div>
  );
}
