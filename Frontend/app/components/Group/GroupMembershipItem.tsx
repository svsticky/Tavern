import { useState } from "react";
import type { GroupMembershipResponseDto } from "~/api";

/**
 * Renders an individual group membership item with its own state for image handling.
 *
 * @param {Object} props - The component props.
 * @param {GroupMembershipResponseDto} props.groupMembership - The specific membership data object.
 * @param {string} props.fallbackUrl - The URL to use if the group picture fails to load.
 * @returns {JSX.Element} A single membership row.
 */
export default function GroupMembershipItem({
  groupMembership,
  fallbackUrl,
}: {
  groupMembership: GroupMembershipResponseDto;
  fallbackUrl: string;
}) {
  const [imageUrl, setImageUrl] = useState(
    `${import.meta.env.ApiUrl}/api/groups/${groupMembership.groupId}/group-picture`,
  );

  return (
    <div className="flex p-2 gap-2">
      <div className="bg-[color-mix(in_srgb,var(--board-primary),white_80%)] rounded-xl w-10 h-10 p-1 flex items-center justify-center">
        <img
          src={imageUrl}
          onError={() => setImageUrl(fallbackUrl)}
          alt={`${groupMembership.memberName} Icon`}
          className="h-full m-auto"
        />
      </div>

      <div>
        <p className="truncate mt-[-2.5px]">
          {groupMembership.groupName} -{" "}
          {`${groupMembership.membershipYear - 1}/${groupMembership.membershipYear}`}
        </p>
        <p className="text-gray-500 mt-[-2.5px]">
          {groupMembership.roleAliasName}
        </p>
      </div>
    </div>
  );
}
