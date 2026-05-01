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
 * @returns {JSX.Element} A list of group memberships or a "No Content" state.
 */
export default function GroupMembershipOverview({
  groupMemberships,
}: {
  groupMemberships: GroupMembershipResponseDto[];
}) {
  if (groupMemberships.length === 0) {
    return <NoContentTile text={t("no_group_enrollments")} />;
  }

  const fallbackUrl = "/profile-picture.svg";

  return (
    <ListTile className="w-full">
      {groupMemberships.map((groupMembership) => (
        <GroupMembershipItem
          key={groupMembership.id}
          groupMembership={groupMembership}
          fallbackUrl={fallbackUrl}
        />
      ))}
    </ListTile>
  );
}
