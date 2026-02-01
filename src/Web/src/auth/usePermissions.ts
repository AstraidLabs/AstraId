import { useMemo } from "react";
import type { User } from "oidc-client-ts";
import { useAuth } from "react-oidc-context";

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

const getPermissionClaims = (user?: User | null): string[] => {
  const claim = user?.profile?.permission as PermissionClaim;
  return normalizePermissions(claim);
};

export const usePermissions = (fallbackPermissions: string[] = []) => {
  const auth = useAuth();

  const permissions = useMemo(() => {
    const directPermissions = getPermissionClaims(auth.user);
    const combined =
      directPermissions.length > 0 ? directPermissions : fallbackPermissions;

    return Array.from(new Set(combined)).sort();
  }, [auth.user, fallbackPermissions]);

  const hasPermission = (permission: string) =>
    permissions.includes(permission);

  return { permissions, hasPermission };
};
