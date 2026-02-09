export const ADMIN_PERMISSION = "system.admin";

export const GDPR_PERMISSIONS = [
  "gdpr.read",
  "gdpr.export",
  "gdpr.erase",
  "gdpr.retention.manage"
] as const;

export const hasAdminAccess = (permissions?: string[]) =>
  permissions?.includes(ADMIN_PERMISSION) ?? false;

export const hasAnyPermission = (required: readonly string[], permissions?: string[]) =>
  required.some((permission) => permissions?.includes(permission));
