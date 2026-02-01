import { NavLink } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import Container from "./Container";
import { usePermissions } from "../auth/usePermissions";

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `rounded-full px-3 py-1 text-sm transition ${
    isActive
      ? "bg-indigo-500/20 text-indigo-200"
      : "text-slate-300 hover:text-white"
  }`;

const TopNav = () => {
  const auth = useAuth();
  const { hasPermission } = usePermissions();

  const handleLogin = () => {
    void auth.signinRedirect();
  };

  const handleLogout = () => {
    void auth.signoutRedirect();
  };

  return (
    <header className="border-b border-slate-800 bg-slate-950/70 backdrop-blur">
      <Container>
        <div className="flex flex-col gap-4 py-5 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="text-xs uppercase tracking-[0.3em] text-slate-500">
              AstraId
            </p>
            <h1 className="text-xl font-semibold text-white">OIDC Web</h1>
          </div>
          <nav className="flex flex-wrap items-center gap-2">
            <NavLink to="/" className={linkClass}>
              Home
            </NavLink>
            <NavLink to="/profile" className={linkClass}>
              Profile
            </NavLink>
            {hasPermission("system.admin") ? (
              <NavLink to="/admin" className={linkClass}>
                Admin
              </NavLink>
            ) : null}
            <NavLink to="/integrations" className={linkClass}>
              Integrations
            </NavLink>
          </nav>
          <div className="flex flex-wrap items-center gap-3">
            {auth.isAuthenticated ? (
              <div className="text-sm text-slate-300">
                {auth.user?.profile?.name ?? "Přihlášen"}
              </div>
            ) : (
              <span className="text-sm text-slate-500">Nepřihlášen</span>
            )}
            {!auth.isAuthenticated ? (
              <button
                className="rounded-full bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400"
                onClick={handleLogin}
              >
                Login
              </button>
            ) : (
              <button
                className="rounded-full border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500"
                onClick={handleLogout}
              >
                Logout
              </button>
            )}
          </div>
        </div>
      </Container>
    </header>
  );
};

export default TopNav;
