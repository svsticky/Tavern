/**
 * The 12 permissions Tavern's own backend understands and enforces, matching
 * Backend/Models/Permission.cs. Groups and Roles can also be granted arbitrary custom
 * permission strings (for other applications sharing this Keycloak instance to read out of the
 * group_memberships claim) - those are handled as plain strings, not part of this type, since
 * Tavern's frontend never needs to check for them.
 */
export const ALL_PERMISSIONS = [
  "EditActivityForGroup",
  "EditAllActivities",
  "ViewFinances",
  "ManageFinances",
  "ViewMembers",
  "ManageMembers",
  "ManageGroups",
  "ManageRoles",
  "ManageGroupPermissions",
  "ManageRolePermissions",
  "EditAnnouncements",
  "ViewPastActivities",
] as const;

export type Permission = (typeof ALL_PERMISSIONS)[number];
