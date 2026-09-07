import type { ActivityResponseDto } from "~/api/types.gen";
import { isAdminModeActive } from "~/context/AdminModeContext";
import type { Permission } from "~/types/Permission";
import type { TokenParsed } from "~/types/TokenParsed";

/**
 * Checks if the current user has genuine board member or admin privileges on their token.
 * Does not respect the temporary client-side member view mode toggle.
 */
export const isActualBoardOrAdmin = (
  tokenParsed: TokenParsed | null,
): boolean => {
  if (!tokenParsed) return false;
  return tokenParsed.is_admin ?? false;
};

/**
 * Checks if the current user is a board member or candidate board member.
 * If the user has switched to member view mode, this returns false so they can preview the site as a regular member.
 * @param tokenParsed The parsed token.
 * @returns True if the user is a board member and admin mode is active, false otherwise.
 */
export const isBoardOrCandidateBoard = (
  tokenParsed: TokenParsed | null,
): boolean => {
  if (!tokenParsed) return false;
  if (!tokenParsed.is_admin) return false;

  return isAdminModeActive();
};

/**
 * Checks if the current user is in a specific group by ID, in the current committee year.
 * @param tokenParsed The parsed token.
 * @returns True if the user is in the specified group, false otherwise.
 */
export const isInGroupWithId = (
  tokenParsed: TokenParsed | null,
  groupId: number,
): boolean => {
  return tokenParsed?.group_memberships?.some((g) => g.id === groupId) ?? false;
};

/**
 * The one permission whose effect is scoped to the group it was granted through - mirrors
 * the backend's PermissionService.GroupScopedPermissions. Every other permission has a
 * global effect: granting it via any group/role membership makes it true everywhere.
 */
const GROUP_SCOPED_PERMISSIONS = new Set<Permission>(["EditActivityForGroup"]);

/**
 * Checks whether the current user has a given permission, either granted directly to a
 * group they're in (in the current committee year), or granted to the role they hold
 * within such a membership. Board and candidate board members always have every permission.
 *
 * @param tokenParsed The parsed token.
 * @param permission The permission to check.
 * @param groupId For the group-scoped `EditActivityForGroup` permission, restricts the
 * check to this group. Ignored for every other (global-effect) permission.
 */
export const hasPermission = (
  tokenParsed: TokenParsed | null,
  permission: Permission,
  groupId?: number,
): boolean => {
  if (isBoardOrCandidateBoard(tokenParsed)) return true;
  if (!tokenParsed?.group_memberships) return false;

  if (!isAdminModeActive()) {
    const ADMIN_PERMISSIONS = new Set<Permission>([
      "EditAllActivities",
      "ManageMembers",
      "ViewMembers",
      "ManageGroups",
      "ManageRoles",
      "ManageRolePermissions",
      "ManageFinances",
      "ViewFinances",
      "EditAnnouncements",
    ]);
    if (ADMIN_PERMISSIONS.has(permission)) return false;
  }

  const memberships =
    GROUP_SCOPED_PERMISSIONS.has(permission) && groupId !== undefined
      ? tokenParsed.group_memberships.filter((g) => g.id === groupId)
      : tokenParsed.group_memberships;

  return memberships.some(
    (g) =>
      Boolean(g.permissions?.includes(permission)) ||
      Boolean(g.role?.permissions?.includes(permission)),
  );
};

/**
 * Gets the IDs of every group in which the current user currently holds the given
 * permission (via a direct group grant or via their role in that group), in the current
 * committee year. Useful for e.g. "which groups can I create/edit activities for".
 *
 * @param tokenParsed The parsed token.
 * @param permission The permission to check.
 */
export const getGroupIdsWithPermission = (
  tokenParsed: TokenParsed | null,
  permission: Permission,
): number[] => {
  return (
    tokenParsed?.group_memberships
      ?.filter(
        (g) =>
          Boolean(g.permissions?.includes(permission)) ||
          Boolean(g.role?.permissions?.includes(permission)),
      )
      .map((g) => g.id) ?? []
  );
};

/**
 * Determines if the current user has permission to edit a specific activity.
 *
 * Logic:
 * - **Board/candidate board members**: Always allowed to edit.
 * - **EditAllActivities holders**: Always allowed to edit, any activity.
 * - **EditActivityForGroup holders**: Allowed only if the activity hasn't been finalized
 *   for external systems (Website/Koala), the event hasn't started yet, and the
 *   permission is held for the activity's organizing group.
 *
 * @param {ActivityResponseDto} activity - The activity data to check against.
 * @param {TokenParsed} tokenParsed - The parsed token containing user roles and ID.
 * @returns {boolean} True if the user is authorized to edit.
 */
export const canEditActivity = (
  activity: ActivityResponseDto,
  tokenParsed: TokenParsed,
) => {
  if (isBoardOrCandidateBoard(tokenParsed)) return true;
  if (hasPermission(tokenParsed, "EditAllActivities")) return true;

  return Boolean(
    !activity.showInKoala &&
      !activity.showOnWebsite &&
      activity.organizerId &&
      hasPermission(
        tokenParsed,
        "EditActivityForGroup",
        activity.organizerId,
      ) &&
      new Date(activity.dateTimeStart) > new Date(Date.now()),
  );
};
