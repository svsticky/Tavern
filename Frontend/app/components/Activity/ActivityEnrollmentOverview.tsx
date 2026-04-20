import { CircleCheckBig } from "lucide-react";
import { formatDate } from "~/util/date.util";
import { ListTile } from "../Tiles/ListTile";
import type { Activity, ActivityResponseDto } from "~/api";
import { t } from "i18next";
import { Link } from "react-router";
import { NoContentTile } from "../Tiles/NoContentTile";
import Tile from "../Tiles/Tile";

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
          <Tile key={activity.id} className="p-0">
            <Link key={activity.id} className="!text-black mt-0 flex p-2 gap-2 hover:bg-gray-50 mt-0" to={`/activities/${activity.id}`}>
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
