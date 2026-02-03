export const routerBase = (import.meta.env.VITE_ROUTER_BASE ?? "/") as string;

export const adminMountPath = "/admin";
export const adminRoutePrefix = routerBase === adminMountPath ? "" : adminMountPath;

const normalizePath = (path: string) => {
  if (!path) {
    return "/";
  }

  return path.startsWith("/") ? path : `/${path}`;
};

export const toAdminRoute = (path: string) => {
  const normalized = normalizePath(path);
  if (!adminRoutePrefix) {
    return normalized;
  }

  if (normalized === "/") {
    return adminRoutePrefix;
  }

  return `${adminRoutePrefix}${normalized}`;
};

export const stripAdminRoutePrefix = (path: string) => {
  if (adminRoutePrefix && path.startsWith(adminRoutePrefix)) {
    const stripped = path.slice(adminRoutePrefix.length);
    return stripped ? normalizePath(stripped) : "/";
  }

  return normalizePath(path);
};

export const toAdminReturnUrl = (path: string) => {
  const normalized = stripAdminRoutePrefix(path);
  if (normalized === "/") {
    return adminMountPath;
  }

  return `${adminMountPath}${normalized}`;
};

export const adminRoutePattern = adminRoutePrefix ? `${adminRoutePrefix}/*` : "/*";
