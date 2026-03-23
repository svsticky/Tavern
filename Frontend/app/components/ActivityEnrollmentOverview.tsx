import { CircleCheckBig } from "lucide-react";
import { formatDate } from "~/util/date.util";
import { ListTile } from "./Tiles/ListTile";
import { NoContentTile } from "./Tiles/NoContentTile";
import type { Activity } from "~/api";

type ActivityEnrollmentOverviewProps = {
  enrolledActivities: Activity[];
};

export default function ActivityEnrollmentOverview({
  enrolledActivities,
}: ActivityEnrollmentOverviewProps) {
  if(enrolledActivities.length === 0) {
    return (
      <NoContentTile text="Je bent momenteel niet ingeschreven voor activiteiten." />
    );
  }
  
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
            <p className="truncate">{activity.name}</p>
            <p className="text-gray-500">
              {formatDate(new Date(activity.dateTimeStart ?? new Date()), "shortDate")}
            </p>
          </div>
        </div>
      ))}
    </ListTile>
  );
}
