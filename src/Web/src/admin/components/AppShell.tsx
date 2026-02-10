import type { ReactNode } from "react";
import { Link, NavLink } from "react-router-dom";
import {
  ADMIN_PERMISSION,
  hasAnyPermission,
  GDPR_PERMISSIONS,
  GOVERNANCE_PERMISSIONS,
} from "../../auth/adminAccess";
import { useAuthSession } from "../../auth/useAuthSession";
import { toAdminRoute } from "../../routing";

type Props = {
  children: ReactNode;
};

type NavItem = {
  to: string;
  label: string;
  end?: boolean;
};

type NavSectionProps = {
  title: string;
  items: NavItem[];
};

const navItemClass = ({ isActive }: { isActive: boolean }) =>
  `rounded-md px-3 py-2 text-sm font-medium transition ${
    isActive ? "bg-slate-800 text-white" : "text-slate-300 hover:bg-slate-900"
  }`;

function NavSection({ title, items }: NavSectionProps) {
  return (
    <div className="flex flex-col gap-2">
      <span className="px-3 text-xs font-semibold uppercase tracking-wide text-slate-500">
        {title}
      </span>
      <div className="flex flex-col gap-1">
        {items.map((item) => (
          <NavLink key={item.to} to={item.to} className={navItemClass} end={item.end}>
            {item.label}
          </NavLink>
        ))}
      </div>
    </div>
  );
}

export default function AppShell({ children }: Props) {
  const { session } = useAuthSession();
  const showGdpr = hasAnyPermission(GDPR_PERMISSIONS, session?.permissions);
  const showUserLifecycle = hasAnyPermission(
    [ADMIN_PERMISSION, GOVERNANCE_PERMISSIONS.userLifecycleManage],
    session?.permissions
  );
  const showInactivityPolicy = hasAnyPermission(
    [ADMIN_PERMISSION, GOVERNANCE_PERMISSIONS.inactivityManage],
    session?.permissions
  );

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <div className="flex min-h-screen">
        <aside className="w-64 border-r border-slate-900 bg-slate-950/80">
          <div className="px-6 py-6">
            <Link to={toAdminRoute("/")} className="text-lg font-semibold text-white">
              AstraId Admin
            </Link>
            <p className="mt-1 text-xs text-slate-500">Authorization server control panel</p>
          </div>
          <nav className="flex flex-col gap-6 px-4 pb-6">
            <NavSection
              title="OIDC"
              items={[
                { to: toAdminRoute("/"), label: "Overview", end: true },
                { to: toAdminRoute("/oidc/clients"), label: "Clients" },
                { to: toAdminRoute("/oidc/scopes"), label: "Scopes" },
                { to: toAdminRoute("/oidc/resources"), label: "Resources" },
              ]}
            />
            <NavSection
              title="Konfigurace"
              items={[
                { to: toAdminRoute("/config/permissions"), label: "Permissions" },
                { to: toAdminRoute("/config/roles"), label: "Roles" },
                { to: toAdminRoute("/config/api-resources"), label: "API Endpoints" },
              ]}
            />
            <NavSection
              title="Uživatelé"
              items={[{ to: toAdminRoute("/users"), label: "Users" }]}
            />
            <NavSection
              title="Audit"
              items={[{ to: toAdminRoute("/audit"), label: "Audit log" }]}
            />
            <NavSection
              title="Security"
              items={[
                { to: toAdminRoute("/security/keys"), label: "Signing Keys" },
                { to: toAdminRoute("/security/rotation"), label: "Rotation Policy" },
                { to: toAdminRoute("/security/tokens"), label: "Token Policy" },
                { to: toAdminRoute("/security/incidents"), label: "Incidents" },
                { to: toAdminRoute("/security/revocation"), label: "Revocation" },
                { to: toAdminRoute("/security/dataprotection"), label: "DataProtection" },
                ...(showUserLifecycle ? [{ to: toAdminRoute("/security/user-lifecycle"), label: "User Lifecycle" }] : []),
                ...(showInactivityPolicy ? [{ to: toAdminRoute("/security/inactivity"), label: "Inactivity Policy" }] : []),
                ...(showGdpr ? [{ to: toAdminRoute("/security/privacy"), label: "Privacy & GDPR" }] : []),
              ]}
            />
            <NavSection
              title="Diagnostics"
              items={[{ to: toAdminRoute("/diagnostics/errors"), label: "Errors" }, { to: toAdminRoute("/diagnostics/email-outbox"), label: "Email Outbox" }]}
            />
          </nav>
        </aside>
        <div className="flex min-h-screen flex-1 flex-col">
          <header className="border-b border-slate-900 bg-slate-950/60 px-8 py-4">
            <div className="text-sm text-slate-400">
              <span className="font-semibold text-slate-200">Admin</span> · AuthServer
            </div>
          </header>
          <main className="flex w-full flex-1 flex-col gap-6 px-8 py-8">{children}</main>
        </div>
      </div>
    </div>
  );
}
