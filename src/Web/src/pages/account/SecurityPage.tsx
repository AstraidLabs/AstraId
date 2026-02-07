import { Link, NavLink, Outlet, useLocation } from "react-router-dom";
import AccountPageHeader from "../../components/account/AccountPageHeader";
import { accountCardIconClass, securityItems } from "../../ui/accountIcons";

export default function SecurityPage() {
  const location = useLocation();
  const isOverview = location.pathname === "/account/security";

  return (
    <div className="space-y-4">
      <AccountPageHeader title="Security" description="Manage account protections with dedicated security controls." />
      <div className="flex flex-wrap gap-2">
        {securityItems.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink key={item.key} to={item.to} className={({ isActive }) => `rounded-lg border px-3 py-2 text-xs font-semibold uppercase tracking-wide ${isActive ? "border-indigo-400 bg-indigo-500/20 text-indigo-100" : "border-slate-700 text-slate-300 hover:border-slate-500"}`}>
              <span className="inline-flex items-center gap-1"><Icon className="h-4 w-4" />{item.label}</span>
            </NavLink>
          );
        })}
      </div>

      {isOverview ? (
        <div className="grid gap-3 md:grid-cols-2">
          {securityItems.map((item) => {
            const Icon = item.icon;
            return (
              <Link key={item.key} to={item.to} className="rounded-xl border border-slate-700 bg-slate-950/40 p-4 text-sm text-slate-100 transition hover:border-indigo-400">
                <Icon className={`${accountCardIconClass} mb-2 text-indigo-300`} />
                <p className="font-semibold">{item.label}</p>
                <p className="mt-1 text-slate-400">{item.description}</p>
              </Link>
            );
          })}
        </div>
      ) : <Outlet />}
    </div>
  );
}
