import type { TokenParsed } from "~/types/TokenParsed";
import { getCommitteeYear } from "./date.util";
import type { ActivityResponseDto } from "~/api/types.gen";

/**
 * Checks if the current user is in a specific group with an optional role.
 * @param tokenParsed The parsed token.
 * @param group The name of the group to check.
 * @param role The optional role within the group.
 * @param year The optional year of the membership.
 * @returns True if the user is in the specified group, false otherwise.
 */
export const isInGroupWithName = (
  tokenParsed: TokenParsed,
  group: string,
  role?: string,
  year?: number,
): boolean => {
  const targetYear = year !== undefined ? year : getCommitteeYear();

  if (!tokenParsed.group_memberships) return false;

  return tokenParsed.group_memberships.some((g: string) => {
    const [gYear, gGroup, gRole] = g.split(":");

    const gGroupName = gGroup.split(";")[1];
    const gRoleName = gRole.split(";")[1];

    return (
      Number(gYear) === targetYear &&
      gGroupName === group &&
      (role ? gRoleName === role : true)
    );
  });
};

/**
 * Checks if the current user is in a specific group by ID with an optional role.
 * @param tokenParsed The parsed token.
 * @param group The ID of the group to check.
 * @param role The optional role within the group.
 * @param year The optional year of the membership.
 * @returns True if the user is in the specified group, false otherwise.
 */
export const isInGroupWithId = (
  tokenParsed: TokenParsed,
  group: number,
  role?: string,
  year?: number,
): boolean => {
  const targetYear = year !== undefined ? year : getCommitteeYear();

  if (!tokenParsed.group_memberships) return false;

  return tokenParsed.group_memberships.some((g: string) => {
    const [gYear, gGroup, gRole] = g.split(":");

    const gGroupId = gGroup.split(";")[0];
    const gRoleId = gRole.split(";")[0];

    return (
      Number(gYear) === targetYear &&
      Number(gGroupId) === group &&
      (role ? gRoleId === role : true)
    );
  });
};

/**
 * Checks if the current user is a board member or candidate board member.
 * @param tokenParsed The parsed token.
 * @returns True if the user is a board member or candidate board member, false otherwise.
 */
export const isBoardOrCandidateBoard = (
  tokenParsed: TokenParsed | null,
): boolean => {
  if (!tokenParsed) return false;

  return tokenParsed.is_admin ?? false;
};

/**
 * Determines if the current user has permission to edit a specific activity.
 *
 * Logic:
 * - **Board Members**: Always allowed to edit.
 * - **Organizers**: Allowed only if the activity hasn't been finalized for
 *   external systems (Website/Koala) and the event hasn't started yet and is part of
 *   the organizing group
 *
 * @param {ActivityResponseDto} activity - The activity data to check against.
 * @param {TokenParsed} tokenParsed - The parsed token containing user roles and ID.
 * @returns {boolean} True if the user is authorized to edit.
 */
export const canEditActivity = (
  activity: ActivityResponseDto,
  tokenParsed: TokenParsed,
) => {
  return (
    isBoardOrCandidateBoard(tokenParsed) ||
    Boolean(
      !activity.showInKoala &&
        !activity.showOnWebsite &&
        activity.organizerId && isInGroupWithId(tokenParsed, activity.organizerId) &&
        new Date(activity.dateTimeStart) > new Date(Date.now()),
    )
  );
};