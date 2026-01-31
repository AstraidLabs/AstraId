import type { User } from "oidc-client-ts";

type PermissionClaim = string | string[] | undefined;

const normalizePermissions = (claim: PermissionClaim): string[] => {
  if (!claim) {
    return [];
  }

  if (Array.isArray(claim)) {
    return claim.map((value) => String(value));
  }

  return [String(claim)];
};

export const getPermissionClaims = (user?: User | null): string[] => {
  const claim = user?.profile?.permission as PermissionClaim;
  return normalizePermissions(claim);
};

export const hasPermission = (
  user: User | null | undefined,
  permission: string,
  fallbackPermissions: string[] = []
): boolean => {
  const directPermissions = getPermissionClaims(user);
  const permissionsToCheck =
    directPermissions.length > 0 ? directPermissions : fallbackPermissions;

  return permissionsToCheck.includes(permission);
};
