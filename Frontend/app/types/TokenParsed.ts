import type { UUID } from "node:crypto";

/**
 * A single entry of the `group_memberships` claim: a group the member belongs to in the
 * current committee year, the permissions granted directly to that group, and (when the
 * membership has a role) the role held and the permissions granted to that role.
 *
 * Each permission entry is either the string name of one of the 12 known `Permission` values, or
 * an arbitrary custom string granted for other applications sharing this Keycloak instance to
 * read - Tavern's own frontend only ever checks for the known ones via `hasPermission`.
 */
export type GroupMembershipClaim = {
  id: number;
  name: string;
  permissions: string[];
  role: {
    id: number;
    name: string;
    alias: string;
    permissions: string[];
  } | null;
};

export type TokenParsed = {
  locale: string;
  UserId: UUID;
  access_level: string;
  group_memberships?: GroupMembershipClaim[];
  given_name: string;
  family_name: string;
  name: string;
  email?: string;
  is_admin?: boolean;
  full_name?: string;
  birthday?: string;
};
