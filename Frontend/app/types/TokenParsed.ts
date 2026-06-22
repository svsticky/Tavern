import type { UUID } from "node:crypto";

export type TokenParsed = {
  locale: string;
  UserId: UUID;
  access_level: string;
  group_memberships?: string[];
  given_name: string;
  family_name: string;
  name: string;
  email?: string;
  is_admin?: boolean;
  full_name?: string;
  birthday?: string;
};
