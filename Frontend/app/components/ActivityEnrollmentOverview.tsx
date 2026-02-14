import { CircleCheckBig } from "lucide-react";
import type { Activity } from "~/types/Activity";
import { formatDate } from "~/util/date.util";
import { ListTile } from "./Tiles/ListTile";

type ActivityEnrollmentOverviewProps = {
  enrolledActivities: Activity[];
};

export default function ActivityEnrollmentOverview({
  enrolledActivities,
}: ActivityEnrollmentOverviewProps) {
  return (
    <ListTile className="w-full">
      {enrolledActivities.map((activity) => (
        <div key={activity.id} className="flex p-2 gap-2">
          {/* Icon Container */}
          <div className="bg-[color-mix(in_srgb,var(--board-primary),white_80%)] rounded-xl w-10 h-10 flex items-center justify-center">
            <CircleCheckBig className="text-(--board-primary) h-full" />
          </div>

          {/* Activity Details */}
          <div>
            <p className="truncate">{activity.title}</p>
            <p className="text-gray-500">
              {formatDate(activity.startdate, "shortDate")}
            </p>
          </div>
        </div>
      ))}
    </ListTile>
  );
}
