import type { TokenParsed } from "~/types/TokenParsed";
import { getFinancialYear, getBoardYear } from "./date.util";

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
  const targetYear = year !== undefined ? year : getFinancialYear();

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
  const targetYear = year !== undefined ? year : getFinancialYear();

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
 * Checks if the current user is a board member.
 * @param tokenParsed The parsed token.
 * @returns True if the user is a board member, false otherwise.
 */
export const isBoard = (
  tokenParsed: TokenParsed,
  boardGroupId: number | null,
): boolean => {
  if (!boardGroupId) return false;

  return isInGroupWithId(tokenParsed, boardGroupId, undefined, getBoardYear());
};

/**
 * Checks if the current user is a board member or candidate board member.
 * @param tokenParsed The parsed token.
 * @returns True if the user is a board member or candidate board member, false otherwise.
 */
export const isBoardOrCandidateBoard = (
  tokenParsed: TokenParsed | null,
  boardGroupId: number | null,
  candidateBoardGroupId: number | null,
): boolean => {
  if (!tokenParsed) return false;

  const boardYear = getBoardYear();
  const inBoard = boardGroupId ? isInGroupWithId(tokenParsed, boardGroupId, undefined, boardYear) : false;
  const inCandidate = candidateBoardGroupId ? isInGroupWithId(tokenParsed, candidateBoardGroupId, undefined, boardYear) : false;

  return inBoard || inCandidate;
};
