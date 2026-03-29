import type { KeycloakTokenParsed } from "keycloak-js";
import { getAssociationYear } from "./date.util";

export const isInGroupWithName = (
  tokenParsed: KeycloakTokenParsed | undefined,
  group: string,
  role?: string
): boolean => {
  if (!tokenParsed?.group_memberships) return false;

  const year = getAssociationYear();

  return tokenParsed.group_memberships.some((g: string) => {
    const [gYear, gGroup, gRole] = g.split(':');

    const gGroupName = gGroup.split(';')[1];
    const gRoleName = gRole.split(';')[1];

    return (
      Number(gYear) === year &&
      gGroupName === group &&
      (role ? gRoleName === role : true)
    );
  });
};

export const isInGroupWithId = (
  tokenParsed: KeycloakTokenParsed | undefined,
  group: number,
  role?: string
): boolean => {
  if (!tokenParsed?.group_memberships) return false;

  const year = getAssociationYear();

  return tokenParsed.group_memberships.some((g: string) => {
    const [gYear, gGroup, gRole] = g.split(':');

    const gGroupId = gGroup.split(';')[0];
    const gRoleId = gRole.split(';')[0];

    return (
      Number(gYear) === year &&
      Number(gGroupId) == group &&
      (role ? gRoleId === role : true)
    );
  });
};