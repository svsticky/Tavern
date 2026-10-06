import { useState } from "react";
import { Link } from "react-router";
import type { GroupMembershipResponseDto } from "~/api";
import { getEnv } from "~/util/config.utils";

/**
 * Renders an individual group membership item with its own state for image handling.
 *
 * @param {Object} props - The component props.
 * @param {GroupMembershipResponseDto} props.groupMembership - The specific membership data object.
 * @param {string} props.fallbackUrl - The URL to use if the group picture fails to load.
 * @param {boolean} [props.linkToGroup] - When true, the item links to the group's admin page. Only
 *   pass this on pages board members reach (e.g. the admin edit-member page) since `/admin/groups/:id`
 *   is board-gated.
 * @returns {JSX.Element} A single membership row.
 */
export default function GroupMembershipItem({
  groupMembership,
  fallbackUrl,
  linkToGroup,
}: {
  groupMembership: GroupMembershipResponseDto;
  fallbackUrl: string;
  linkToGroup?: boolean;
}) {
  const [imageUrl, setImageUrl] = useState(
    `${getEnv("ApiUrl")}/groups/${groupMembership.groupId}/group-picture`,
  );

  const content = (
    <div
      className={`flex p-2 gap-2 rounded-lg transition-colors ${linkToGroup ? "cursor-pointer hover:bg-slate-50" : ""}`}
    >
      <div className="bg-[color-mix(in_srgb,var(--board-primary),white_80%)] rounded-xl w-10 h-10 p-1 flex items-center justify-center">
        <img
          src={imageUrl}
          onError={() => setImageUrl(fallbackUrl)}
          alt={`${groupMembership.memberName} Icon`}
          className="w-full h-full object-contain"
        />
      </div>

      <div className="flex-1 min-w-0">
        <p
          className="truncate mt-[-2.5px]"
          title={`${groupMembership.groupName} - ${groupMembership.membershipYear - 1}/${groupMembership.membershipYear}`}
        >
          {groupMembership.groupName} -{" "}
          {`${groupMembership.membershipYear - 1}/${groupMembership.membershipYear}`}
        </p>
        <p className="text-gray-500 mt-[-2.5px] truncate">
          {groupMembership.roleAliasName}
        </p>
      </div>
    </div>
  );

  if (!linkToGroup) return content;

  return (
    <Link
      to={`/admin/groups/${groupMembership.groupId}`}
      className="no-underline text-inherit"
    >
      {content}
    </Link>
  );
}
