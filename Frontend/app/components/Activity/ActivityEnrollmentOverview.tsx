import { t } from "i18next";
import { CircleCheckBig } from "lucide-react";
import { Link } from "react-router";
import type { ActivityResponseDto } from "~/api";
import { formatDate } from "~/util/date.util";
import { ListTile } from "../Tiles/ListTile";
import { NoContentTile } from "../Tiles/NoContentTile";
import Tile from "../Tiles/Tile";

/**
 * A specialized list component that displays a collection of activities
 * the current user is enrolled in.
 *
 * Features:
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
 * representing the user's current enrollments.
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
  if (enrolledActivities.length === 0) {
    return <NoContentTile text={t("no_enrollments")} />;
  }

  return (
    <ListTile className="w-full">
      {enrolledActivities.map((activity) => (
        <Tile key={activity.id} className="p-0">
          <Link
            key={activity.id}
            className="!text-black mt-0 flex p-2 gap-2 hover:bg-gray-50 mt-0"
            to={`/activities/${activity.id}`}
          >
            {/* Icon Container */}
            <div className="bg-[color-mix(in_srgb,var(--board-primary),white_80%)] rounded-xl w-10 h-10 flex items-center justify-center">
              <CircleCheckBig className="text-(--board-primary) h-full" />
            </div>

            {/* Activity Details */}
            <div>
              <p className="truncate mt-[-2.5px]">{activity.name}</p>
              <p className="text-gray-500 mt-[-2.5px]">
                {formatDate(new Date(activity.dateTimeStart), "shortDate")}
              </p>
            </div>
          </Link>
        </Tile>
      ))}
    </ListTile>
  );
}
