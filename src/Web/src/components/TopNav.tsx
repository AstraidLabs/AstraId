import { NavLink, useNavigate } from "react-router-dom";
import Container from "./Container";
import { logout } from "../api/authServer";
import { useAuthSession } from "../auth/useAuthSession";
import { getAdminEntryUrl, isAbsoluteUrl } from "../utils/adminEntry";
import AccountDropdown from "./AccountDropdown";

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `rounded-full px-3 py-1 text-sm transition ${
    isActive
      ? "bg-indigo-500/20 text-indigo-200"
      : "text-slate-300 hover:text-white"
  }`;

const TopNav = () => {
  const navigate = useNavigate();
  const { session, refresh } = useAuthSession();
  const isAdmin = session?.permissions?.includes("system.admin") ?? false;
  const adminUrl = getAdminEntryUrl();
  const adminIsExternal = isAbsoluteUrl(adminUrl);

  const handleLogout = async () => {
    await logout();
    await refresh();
    navigate("/", { replace: true });
  };

  return (
    <header className="border-b border-slate-800 bg-slate-950/70 backdrop-blur">
      <Container>
        <div className="flex flex-col gap-4 py-5 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="text-xs uppercase tracking-[0.3em] text-slate-500">
              AstraId
            </p>
            <h1 className="text-xl font-semibold text-white">Public UI</h1>
          </div>
          <nav className="flex flex-wrap items-center gap-2">
            <NavLink to="/" className={linkClass}>
              Home
            </NavLink>
            <NavLink to="/login" className={linkClass}>
              Login
            </NavLink>
            <NavLink to="/register" className={linkClass}>
              Register
            </NavLink>
            <NavLink to="/forgot-password" className={linkClass}>
              Forgot password
            </NavLink>
            {isAdmin ? (
              adminIsExternal ? (
                <a
                  href={adminUrl}
                  className="rounded-full px-3 py-1 text-sm text-amber-200 transition hover:text-amber-50"
                >
                  Admin
                </a>
              ) : (
                <NavLink to={adminUrl} className={linkClass}>
                  Admin
                </NavLink>
              )
            ) : null}
          </nav>
          <div className="flex flex-wrap items-center gap-3">
            {session && session.isAuthenticated ? (
              <>
                <AccountDropdown session={session} onLogout={handleLogout} />
                <button
                  className="rounded-full border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500"
                  onClick={handleLogout}
                >
                  Logout
                </button>
              </>
            ) : (
              <span className="text-sm text-slate-500">Signed out</span>
            )}
          </div>
        </div>
      </Container>
    </header>
  );
};

export default TopNav;
