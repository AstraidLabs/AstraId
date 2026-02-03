import type { ReactNode } from "react";
import { Link, NavLink } from "react-router-dom";

type Props = {
  children: ReactNode;
};

const navItemClass = ({ isActive }: { isActive: boolean }) =>
  `rounded-md px-3 py-2 text-sm font-medium transition ${
    isActive ? "bg-slate-800 text-white" : "text-slate-300 hover:bg-slate-900"
  }`;

export default function AppShell({ children }: Props) {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <header className="border-b border-slate-800 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <Link to="/" className="text-lg font-semibold text-white">
            AstraId Admin
          </Link>
          <nav className="flex items-center gap-2">
            <NavLink to="/" className={navItemClass} end>
              Dashboard
            </NavLink>
            <NavLink to="/clients" className={navItemClass}>
              Clients
            </NavLink>
            <NavLink to="/scopes" className={navItemClass}>
              Scopes
            </NavLink>
            <NavLink to="/users" className={navItemClass}>
              Users
            </NavLink>
            <NavLink to="/audit" className={navItemClass}>
              Audit
            </NavLink>
          </nav>
        </div>
      </header>
      <main className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
        {children}
      </main>
    </div>
  );
}
