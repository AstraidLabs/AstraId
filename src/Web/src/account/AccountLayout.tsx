import { useMemo, useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import AccountNavItem from "../components/account/AccountNavItem";
import { BackIcon, HomeIcon, LockIcon, MailIcon, MonitorIcon, ShieldIcon, UserIcon } from "../components/account/Icons";
import { useAuthSession } from "../auth/useAuthSession";
import { getAdminEntryUrl, isAbsoluteUrl } from "../utils/adminEntry";

type MenuItem = { to: string; label: string; icon: JSX.Element };

const navClass = ({ isActive }: { isActive: boolean }) =>
  `flex items-center gap-2 rounded-lg px-3 py-2 text-sm transition ${
    isActive ? "bg-indigo-500/20 text-indigo-100" : "text-slate-300 hover:bg-slate-800 hover:text-white"
  }`;

export default function AccountLayout() {
  const [open, setOpen] = useState(false);
  const { session } = useAuthSession();
  const isAdmin = session?.permissions?.includes("system.admin") ?? false;
  const adminUrl = getAdminEntryUrl();
  const adminExternal = isAbsoluteUrl(adminUrl);

  const menuItems = useMemo<MenuItem[]>(() => [
    { to: "/account/overview", label: "Overview", icon: <HomeIcon /> },
    { to: "/account/profile", label: "Profile", icon: <UserIcon /> },
    { to: "/account/password", label: "Password", icon: <LockIcon /> },
    { to: "/account/email", label: "Email", icon: <MailIcon /> },
    { to: "/account/sessions", label: "Sessions", icon: <MonitorIcon /> },
    { to: "/account/mfa", label: "MFA", icon: <ShieldIcon /> },
    { to: "/account/security-events", label: "Security events", icon: <ShieldIcon /> }
  ], []);

  return (
    <div className="grid gap-4 md:grid-cols-[240px,1fr]">
      <aside className="rounded-xl border border-slate-800 bg-slate-900/40 p-3">
        <button
          type="button"
          className="mb-3 w-full rounded-lg border border-slate-700 px-3 py-2 text-left text-sm text-slate-200 md:hidden"
          onClick={() => setOpen((current) => !current)}
        >
          Account menu
        </button>
        <nav className={`${open ? "flex" : "hidden"} flex-col gap-1 md:flex`}>
          {menuItems.map((item) => (
            <AccountNavItem key={item.to} to={item.to} icon={item.icon} label={item.label} onClick={() => setOpen(false)} />
          ))}
          <NavLink to="/" className={navClass}>
            <BackIcon />
            Back to Home
          </NavLink>
          {isAdmin ? (
            adminExternal ? (
              <a href={adminUrl} className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-amber-200 transition hover:bg-slate-800">
                <ShieldIcon />
                Admin
              </a>
            ) : (
              <NavLink to={adminUrl} className={navClass}>
                <ShieldIcon />
                Admin
              </NavLink>
            )
          ) : null}
        </nav>
      </aside>
      <section className="rounded-xl border border-slate-800 bg-slate-900/30 p-6">
        <header className="mb-6 border-b border-slate-800 pb-4">
          <h1 className="text-2xl font-semibold text-white">Account</h1>
        </header>
        <Outlet />
      </section>
    </div>
  );
}
