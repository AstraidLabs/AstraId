import { NavLink, useNavigate } from "react-router-dom";
import Container from "./Container";
import { logout } from "../api/authServer";
import { useAuthSession } from "../auth/useAuthSession";
import { isAuthenticatedSession } from "../auth/sessionState";
import { getAdminEntryUrl, isAbsoluteUrl } from "../utils/adminEntry";
import AccountDropdown from "./AccountDropdown";
import LanguageSelector from "./LanguageSelector";
import { useLanguage } from "../i18n/LanguageProvider";

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `rounded-full px-3 py-1 text-sm transition ${
    isActive
      ? "bg-indigo-500/20 text-indigo-200"
      : "text-slate-300 hover:text-white"
  }`;

const TopNav = () => {
  const navigate = useNavigate();
  const { t } = useLanguage();
  const { session, status, refresh } = useAuthSession();
  const isAuthenticated = isAuthenticatedSession(status, session);
  const isAdmin = session?.permissions?.includes("system.admin") ?? false;
  const adminUrl = getAdminEntryUrl();
  const adminIsExternal = isAbsoluteUrl(adminUrl);

  const handleLogout = async () => {
    await logout();
    await refresh();
    navigate("/", { replace: true });
  };

  return (
    <div className="border-b border-slate-800 bg-slate-950/70 backdrop-blur">
      <Container>
        <div className="flex flex-col gap-4 py-5 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="text-xs uppercase tracking-[0.3em] text-slate-500">
              AstraId
            </p>
            <h1 className="text-xl font-semibold text-white">AstraId</h1>
          </div>
          <nav className="flex flex-wrap items-center gap-2" aria-label="Primary">
            <NavLink to="/" className={linkClass}>
              {t("common.home")}
            </NavLink>
            {status === "loading" ? (
              <span className="rounded-full border border-slate-800 px-3 py-1 text-sm text-slate-500">
                {t("common.loadingSession")}
              </span>
            ) : null}
            {isAuthenticated && isAdmin ? (
              adminIsExternal ? (
                <a
                  href={adminUrl}
                  className="rounded-full px-3 py-1 text-sm text-amber-200 transition hover:text-amber-50"
                >
                  {t("common.admin")}
                </a>
              ) : (
                <NavLink to={adminUrl} className={linkClass}>
                  {t("common.admin")}
                </NavLink>
              )
            ) : null}
          </nav>
          <div className="flex flex-wrap items-center gap-3">
            {status === "loading" ? (
              <span className="text-sm text-slate-500">{t("common.loadingSession")}</span>
            ) : isAuthenticated && session ? (
              <>
                <LanguageSelector authenticated compact />
                <AccountDropdown session={session} onLogout={handleLogout} />
              </>
            ) : (
              <LanguageSelector compact />
            )}
          </div>
        </div>
      </Container>
    </div>
  );
};

export default TopNav;
