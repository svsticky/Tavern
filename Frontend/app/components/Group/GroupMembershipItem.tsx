import { useState } from "react";
import { Link } from "react-router";
import type { GroupMembershipResponseDto } from "~/api";
import { getEnv } from "~/util/config.utils";
import { cn } from "~/util/tailwind.util";

/**
 * Renders an individual group membership item with its own state for image handling.
 *
 * @param {Object} props - The component props.
 * @param {GroupMembershipResponseDto} props.groupMembership - The specific membership data object.
 * @param {string} props.fallbackUrl - The URL to use if the group picture fails to load.
 * @param {boolean} [props.isClickable] - Whether the group membership links to the admin group page.
 * @returns {JSX.Element} A single membership row.
 */
export default function GroupMembershipItem({
  groupMembership,
  fallbackUrl,
  isClickable = false,
}: {
  groupMembership: GroupMembershipResponseDto;
  fallbackUrl: string;
  isClickable?: boolean;
}) {
  const [imageUrl, setImageUrl] = useState(
    `${getEnv("ApiUrl")}/groups/${groupMembership.groupId}/group-picture`,
  );

  const canClick = isClickable && Boolean(groupMembership.groupId);

  const content = (
    <div
      className={cn(
        "flex p-2 gap-2 rounded-xl transition-colors",
        canClick ? "cursor-pointer hover:bg-slate-100 group" : "",
      )}
    >
      <div className="bg-[color-mix(in_srgb,var(--board-primary),white_80%)] rounded-xl w-10 h-10 p-1 flex items-center justify-center flex-shrink-0">
        <img
          src={imageUrl}
          onError={() => setImageUrl(fallbackUrl)}
          alt={`${groupMembership.memberName} Icon`}
          className="w-full h-full object-contain"
        />
      </div>

      <div className="flex-1 min-w-0">
        <p
          className={cn(
            "truncate mt-[-2.5px]",
            canClick &&
              "group-hover:text-(--board-primary) group-hover:underline transition-colors",
          )}
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

  if (canClick) {
    return (
      <Link to={`/admin/groups/${groupMembership.groupId}`} className="block">
        {content}
      </Link>
    );
  }

  return content;
}
