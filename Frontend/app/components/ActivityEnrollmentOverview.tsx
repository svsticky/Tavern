import { CircleCheckBig } from "lucide-react";
import { formatDate } from "~/util/date.util";
import { ListTile } from "./Tiles/ListTile";
import { NoContentTile } from "./Tiles/NoContentTile";
import type { Activity, ActivityResponseDto } from "~/api";
import { t } from "i18next";
import { Link } from "react-router";

type ActivityEnrollmentOverviewProps = {
  enrolledActivities: ActivityResponseDto[];
};

export default function ActivityEnrollmentOverview({
  enrolledActivities,
}: ActivityEnrollmentOverviewProps) {
  if(enrolledActivities.length === 0) {
    return (
      <NoContentTile text={t("no_enrollments")} />
    );
  }
  
  return (
    <ListTile className="w-full">
      {enrolledActivities.map((activity) => (
        <Link key={activity.id} to={`/activities/${activity.id}`}>
          <div key={activity.id} className="flex p-2 gap-2 hover:bg-gray-50 rounded-lg">
            {/* Icon Container */}
            <div className="bg-[color-mix(in_srgb,var(--board-primary),white_80%)] rounded-xl w-10 h-10 flex items-center justify-center">
              <CircleCheckBig className="text-(--board-primary) h-full" />
            </div>

            {/* Activity Details */}
            <div>
              <p className="truncate">{activity.name}</p>
              <p className="text-gray-500">
                {formatDate(new Date(activity.dateTimeStart ?? Date.now()), "shortDate")}
              </p>
            </div>
          </div>
        </Link>
      ))}
    </ListTile>
  );
}
