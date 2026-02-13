import type { LucideIcon } from "lucide-react";
import {
  Activity,
  AppWindow,
  BookCheck,
  Boxes,
  Bug,
  FileWarning,
  FolderSync,
  Gauge,
  KeyRound,
  LifeBuoy,
  Logs,
  ShieldCheck,
  ShieldAlert,
  Users,
} from "lucide-react";
import { matchPath } from "react-router-dom";
import {
  ADMIN_PERMISSION,
  GDPR_PERMISSIONS,
  GOVERNANCE_PERMISSIONS,
  hasAnyPermission,
} from "../auth/adminAccess";
import { stripAdminRoutePrefix } from "../routing";

export type AdminCategoryId =
  | "directory"
  | "applications"
  | "security"
  | "integrations"
  | "diagnostics"
  | "governance";

type AdminRouteMeta = {
  path: string;
  title: string;
  description: string;
};

export type AdminNavItem = {
  id: string;
  label: string;
  path: string;
  category: AdminCategoryId;
  description: string;
  icon: LucideIcon;
  permissionKeys?: readonly string[];
  showInSidebar?: boolean;
  routeMeta?: AdminRouteMeta[];
};

export type AdminCategory = {
  id: AdminCategoryId;
  label: string;
  description: string;
  path: string;
  icon: LucideIcon;
};

export const ADMIN_CATEGORIES: AdminCategory[] = [
  { id: "directory", label: "Directory", description: "Users, roles and permissions", path: "/directory", icon: Users },
  { id: "applications", label: "Applications", description: "Clients, scopes and resources", path: "/apps", icon: AppWindow },
  { id: "security", label: "Security", description: "Token, keys, incidents and audits", path: "/security", icon: ShieldCheck },
  { id: "integrations", label: "Integrations", description: "External channels and connector settings", path: "/integrations", icon: FolderSync },
  { id: "diagnostics", label: "Diagnostics", description: "System diagnostics and failures", path: "/diagnostics", icon: Bug },
  { id: "governance", label: "Privacy & Governance", description: "Data lifecycle and regulatory controls", path: "/governance", icon: BookCheck },
];

export const ADMIN_NAV_ITEMS: AdminNavItem[] = [
  {
    id: "users",
    label: "Users",
    path: "/users",
    category: "directory",
    description: "View and administer user identities.",
    icon: Users,
    routeMeta: [
      { path: "/users", title: "Users", description: "Browse and manage user accounts." },
      { path: "/users/:id", title: "User details", description: "Inspect account profile and role assignments." },
    ],
  },
  {
    id: "roles",
    label: "Roles",
    path: "/config/roles",
    category: "directory",
    description: "Manage role definitions and grants.",
    icon: ShieldCheck,
    routeMeta: [
      { path: "/config/roles", title: "Roles", description: "Manage role definitions and assignments." },
      { path: "/config/roles/:id", title: "Role details", description: "Review role membership and permissions." },
    ],
  },
  {
    id: "permissions",
    label: "Permissions",
    path: "/config/permissions",
    category: "directory",
    description: "Create and maintain permission keys.",
    icon: KeyRound,
    routeMeta: [
      { path: "/config/permissions", title: "Permissions", description: "Manage system permission keys." },
      { path: "/config/permissions/new", title: "Create permission", description: "Register a new permission key." },
      { path: "/config/permissions/:id", title: "Edit permission", description: "Update a permission metadata." },
    ],
  },
  {
    id: "oidc-clients",
    label: "OIDC Clients",
    path: "/oidc/clients",
    category: "applications",
    description: "Manage applications that use AstraId.",
    icon: AppWindow,
    routeMeta: [
      { path: "/oidc/clients", title: "OIDC clients", description: "Manage registered relying party clients." },
      { path: "/oidc/clients/new", title: "Create OIDC client", description: "Register a new OIDC client." },
      { path: "/oidc/clients/:id", title: "Edit OIDC client", description: "Update client settings and credentials." },
    ],
  },
  {
    id: "oidc-scopes",
    label: "Scopes",
    path: "/oidc/scopes",
    category: "applications",
    description: "Define identity scopes exposed to clients.",
    icon: Boxes,
    routeMeta: [
      { path: "/oidc/scopes", title: "OIDC scopes", description: "Manage available scope definitions." },
      { path: "/oidc/scopes/new", title: "Create scope", description: "Define a new OIDC scope." },
      { path: "/oidc/scopes/:id", title: "Edit scope", description: "Edit scope metadata and claims." },
    ],
  },
  {
    id: "oidc-resources",
    label: "Resources",
    path: "/oidc/resources",
    category: "applications",
    description: "Manage OIDC resources and claims.",
    icon: Logs,
    routeMeta: [
      { path: "/oidc/resources", title: "OIDC resources", description: "Manage identity resources and claims." },
      { path: "/oidc/resources/new", title: "Create resource", description: "Define a new identity resource." },
      { path: "/oidc/resources/:id", title: "Edit resource", description: "Update an identity resource." },
    ],
  },
  {
    id: "api-resources",
    label: "API Resources",
    path: "/config/api-resources",
    category: "applications",
    description: "Configure APIs and endpoint exposure.",
    icon: Gauge,
    routeMeta: [
      { path: "/config/api-resources", title: "API resources", description: "Manage API resources and endpoint maps." },
      { path: "/config/api-resources/new", title: "Create API resource", description: "Create a protected API resource." },
      { path: "/config/api-resources/:id", title: "Edit API resource", description: "Update API resource configuration." },
      { path: "/config/api-resources/:id/endpoints", title: "API endpoints", description: "Manage endpoints linked to an API resource." },
    ],
  },
  {
    id: "signing-keys",
    label: "Signing Keys",
    path: "/security/keys",
    category: "security",
    description: "Manage active signing keys and keyring state.",
    icon: KeyRound,
  },
  {
    id: "token-policy",
    label: "Token Policy",
    path: "/security/tokens",
    category: "security",
    description: "Control token lifetimes and reuse protections.",
    icon: ShieldAlert,
  },
  {
    id: "revocation",
    label: "Revocation & Sessions",
    path: "/security/revocation",
    category: "security",
    description: "Revoke tokens and active sessions.",
    icon: Activity,
  },
  {
    id: "rotation",
    label: "Rotation Policy",
    path: "/security/rotation",
    category: "security",
    description: "Configure key rotation cadence.",
    icon: LifeBuoy,
  },
  {
    id: "incidents",
    label: "Incidents",
    path: "/security/incidents",
    category: "security",
    description: "Investigate suspicious token activity.",
    icon: FileWarning,
  },
  {
    id: "audit",
    label: "Audit Logs",
    path: "/audit",
    category: "security",
    description: "Track administrative and security events.",
    icon: Logs,
  },
  {
    id: "data-protection",
    label: "Data Protection",
    path: "/security/dataprotection",
    category: "integrations",
    description: "Data protection key ring and envelope settings.",
    icon: ShieldCheck,
  },
  {
    id: "email-outbox",
    label: "Email Outbox",
    path: "/diagnostics/email-outbox",
    category: "integrations",
    description: "Inspect outbound email integration flow.",
    icon: FolderSync,
  },
  {
    id: "errors",
    label: "Error Logs",
    path: "/diagnostics/errors",
    category: "diagnostics",
    description: "Review runtime errors and diagnostics traces.",
    icon: Bug,
    routeMeta: [
      { path: "/diagnostics/errors", title: "Error logs", description: "Review captured diagnostics errors." },
      { path: "/diagnostics/errors/:id", title: "Error details", description: "Inspect a diagnostics error record." },
    ],
  },
  {
    id: "user-lifecycle",
    label: "User Lifecycle",
    path: "/security/user-lifecycle",
    category: "governance",
    description: "Configure user inactivity and retention lifecycle.",
    icon: Users,
    permissionKeys: [ADMIN_PERMISSION, GOVERNANCE_PERMISSIONS.userLifecycleManage],
  },
  {
    id: "inactivity",
    label: "Inactivity Policy",
    path: "/security/inactivity",
    category: "governance",
    description: "Control inactivity lock and cleanup policies.",
    icon: Activity,
    permissionKeys: [ADMIN_PERMISSION, GOVERNANCE_PERMISSIONS.inactivityManage],
  },
  {
    id: "privacy",
    label: "Privacy & GDPR",
    path: "/security/privacy",
    category: "governance",
    description: "GDPR operations and privacy controls.",
    icon: BookCheck,
    permissionKeys: GDPR_PERMISSIONS,
  },
];

const categoryMetaByPath = new Map(ADMIN_CATEGORIES.map((category) => [category.path, {
  path: category.path,
  title: category.label,
  description: category.description,
}]));

const routeMeta = [
  { path: "/", title: "Admin overview", description: "Authorization administration console." },
  ...ADMIN_NAV_ITEMS.flatMap((item) => item.routeMeta ?? [{ path: item.path, title: item.label, description: item.description }]),
  ...Array.from(categoryMetaByPath.values()),
] as const;

export const canAccessAdminItem = (item: AdminNavItem, permissions?: string[]) => {
  if (!item.permissionKeys || item.permissionKeys.length === 0) {
    return true;
  }

  return hasAnyPermission(item.permissionKeys, permissions);
};

export const getVisibleAdminItems = (permissions?: string[]) =>
  ADMIN_NAV_ITEMS.filter((item) => canAccessAdminItem(item, permissions));

export const getVisibleCategories = (permissions?: string[]) =>
  ADMIN_CATEGORIES.filter((category) =>
    getVisibleAdminItems(permissions).some((item) => item.category === category.id)
  );

export const getRouteMeta = (pathname: string) => {
  const strippedPath = stripAdminRoutePrefix(pathname);

  const exact = routeMeta.find((meta) =>
    matchPath({ path: meta.path, end: true }, strippedPath)
  );

  return exact ?? { path: strippedPath, title: "Admin", description: "Administration" };
};

const titleFromPath = (path: string) =>
  path
    .split("/")
    .filter(Boolean)
    .map((segment) => segment.replace(/-/g, " "))
    .map((segment) => segment.charAt(0).toUpperCase() + segment.slice(1))
    .join(" ");

export const buildBreadcrumbs = (pathname: string) => {
  const strippedPath = stripAdminRoutePrefix(pathname);
  const crumbs: { label: string; path: string }[] = [{ label: "Admin", path: "/" }];

  if (strippedPath === "/") {
    return crumbs;
  }

  const parts = strippedPath.split("/").filter(Boolean);
  let current = "";

  parts.forEach((segment) => {
    current += `/${segment}`;
    const matched = routeMeta.find((meta) =>
      matchPath({ path: meta.path, end: true }, current)
    );

    crumbs.push({
      label: matched?.title ?? titleFromPath(segment),
      path: current,
    });
  });

  return crumbs;
};

export const getCategoryById = (categoryId: AdminCategoryId) =>
  ADMIN_CATEGORIES.find((category) => category.id === categoryId);
