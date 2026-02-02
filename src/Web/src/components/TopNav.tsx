import { NavLink, useNavigate } from "react-router-dom";
import Container from "./Container";
import { logout } from "../api/authServer";
import { useAuthSession } from "../auth/useAuthSession";

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `rounded-full px-3 py-1 text-sm transition ${
    isActive
      ? "bg-indigo-500/20 text-indigo-200"
      : "text-slate-300 hover:text-white"
  }`;

const TopNav = () => {
  const navigate = useNavigate();
  const { session, refresh } = useAuthSession();

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
          </nav>
          <div className="flex flex-wrap items-center gap-3">
            {session && session.isAuthenticated ? (
              <div className="text-sm text-slate-300">
                {session.userName ?? session.email ?? "Přihlášen"}
              </div>
            ) : (
              <span className="text-sm text-slate-500">Nepřihlášen</span>
            )}
            {session && session.isAuthenticated ? (
              <button
                className="rounded-full border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500"
                onClick={handleLogout}
              >
                Logout
              </button>
            ) : null}
          </div>
        </div>
      </Container>
    </header>
  );
};

export default TopNav;
