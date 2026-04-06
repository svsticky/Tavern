import type { CommitteeEnrollment } from "~/types/CommitteeEnrollment";
import { ListTile } from "./Tiles/ListTile";
import { t } from "i18next";
import { NoContentTile } from "./Tiles/NoContentTile";

type CommitteeEnrollmentOverviewProps = {
  committeeEnrollments: CommitteeEnrollment[];
};

export default function CommitteeEnrollmentOverview({
  committeeEnrollments,
}: CommitteeEnrollmentOverviewProps) {
  if(committeeEnrollments.length === 0) {
    return (
      <NoContentTile text={t("no_group_enrollments")} />
    );
  }

  return (
    <ListTile className="w-full">
      {committeeEnrollments.map((committee) => (
        <div key={committee.id} className="flex p-2 gap-2">
          {/* Icon Container */}
          <div className="bg-[color-mix(in_srgb,var(--board-primary),white_80%)] rounded-xl w-10 h-10 p-1 flex items-center justify-center">
            <img
              src={committee.icon}
              alt={`${committee.name} Icon`}
              className="h-full m-auto"
            />
          </div>

          {/* Committee Details */}
          <div>
            <p className="truncate">{committee.name}</p>
            <p className="text-gray-500">{committee.role}</p>
          </div>
        </div>
      ))}
    </ListTile>
  );
}
