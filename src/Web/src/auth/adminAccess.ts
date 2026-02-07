export const ADMIN_PERMISSION = "system.admin";

export const hasAdminAccess = (permissions?: string[]) =>
  permissions?.includes(ADMIN_PERMISSION) ?? false;
