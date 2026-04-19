import { ListTile } from "../Tiles/ListTile";
import { t } from "i18next";
import { NoContentTile } from "../Tiles/NoContentTile";
import type { GroupMembershipResponseDto } from "~/api";
import { useState } from "react";

type CommitteeEnrollmentOverviewProps = {
  groupMemberships: GroupMembershipResponseDto[];
};

export default function GroupMembershipOverview({
  groupMemberships: groupMemberships,
}: CommitteeEnrollmentOverviewProps) {
  if(groupMemberships.length === 0) {
    return (
      <NoContentTile text={t("no_group_enrollments")} />
    );
  }

  const fallbackUrl = "/profile-picture.svg";

  return (
    <ListTile className="w-full">
      {groupMemberships.map((groupMembership) => {
        const [imageUrl, setImageUrl] = useState(`${import.meta.env.ApiUrl}/api/groups/${groupMembership.groupId}/group-picture`);

        return (
        <div key={groupMembership.id} className="flex p-2 gap-2">
          {/* Icon Container */}
          <div className="bg-[color-mix(in_srgb,var(--board-primary),white_80%)] rounded-xl w-10 h-10 p-1 flex items-center justify-center">
            <img
              src={imageUrl}
              onError={() => setImageUrl(fallbackUrl)}
              alt={`${groupMembership.memberName} Icon`}
              className="h-full m-auto"
            />
          </div>

          {/* Group Details */}
          <div>
            <p className="truncate mt-[-2.5px]">{groupMembership.groupName} - {`${groupMembership.membershipYear - 1}/${groupMembership.membershipYear}`}</p>
            <p className="text-gray-500 mt-[-2.5px]">{groupMembership.roleAliasName}</p>
          </div>
        </div>
      )})}
    </ListTile>
  );
}
