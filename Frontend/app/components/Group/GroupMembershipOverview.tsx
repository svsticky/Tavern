import { t } from "i18next";
import type { GroupMembershipResponseDto } from "~/api";
import { ListTile } from "../Tiles/ListTile";
import { NoContentTile } from "../Tiles/NoContentTile";
import GroupMembershipItem from "./GroupMembershipItem";

/**
 * A component that renders a list of a user's group memberships.
 * Displays group icons, names, academic years, and specific roles.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {GroupMembershipResponseDto[]} props.groupMemberships - An array of membership data objects from the API.
 * @param {string} [props.emptyText] - Optional override for the empty-state message (defaults to the
 *   first-person "you are not enrolled" copy used on the home page; pass a third-person alternative when
 *   showing another member's memberships, e.g. on the admin edit-member page).
 * @returns {JSX.Element} A list of group memberships or a "No Content" state.
 */
export default function GroupMembershipOverview({
  groupMemberships,
  emptyText,
  isClickable,
}: {
  groupMemberships: GroupMembershipResponseDto[];
  emptyText?: string;
  isClickable?: boolean;
}) {
  if (groupMemberships.length === 0) {
    return <NoContentTile text={emptyText ?? t("no_group_enrollments")} />;
  }

  const fallbackUrl = "/profile-picture.svg";

  const sortedGroupMemberships = [...groupMemberships].sort(
    (a, b) => b.membershipYear - a.membershipYear,
  );

  return (
    <ListTile className="w-full">
      {sortedGroupMemberships.map((groupMembership) => (
        <GroupMembershipItem
          key={groupMembership.id}
          groupMembership={groupMembership}
          fallbackUrl={fallbackUrl}
          isClickable={isClickable}
        />
      ))}
    </ListTile>
  );
}
