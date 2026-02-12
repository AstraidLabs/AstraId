import { useMemo } from "react";
import { Link } from "react-router-dom";
import Card from "../components/Card";
import Alert from "../components/Alert";
import { useAuthSession } from "../auth/useAuthSession";
import { getAdminEntryUrl, isAbsoluteUrl } from "../utils/adminEntry";
import { useLanguage } from "../i18n/LanguageProvider";

const Home = () => {
  const { t } = useLanguage();
  const { session, isLoading, error } = useAuthSession();
  const adminUrl = getAdminEntryUrl();
  const adminIsExternal = isAbsoluteUrl(adminUrl);

  const isAdmin = useMemo(() => {
    if (!session?.permissions?.length) {
      return false;
    }
    return session.permissions.some((permission) => permission.toLowerCase() === "system.admin");
  }, [session]);

  return (
    <div className="flex flex-col gap-6">
      <Card title={t("home.title")} description={t("home.description")}>
        <div className="flex flex-col gap-3 text-sm text-slate-300">
          {isLoading ? (
            <Alert variant="info">{t("home.loading")}</Alert>
          ) : error ? (
            <Alert variant="warning">{error}</Alert>
          ) : session && session.isAuthenticated ? (
            <>
              <p className="text-base font-semibold text-white">{t("home.authenticated")}</p>
              <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
                <p className="text-sm text-slate-400">{t("home.user")}</p>
                <p className="text-sm text-white">{session.userName ?? session.email ?? t("common.unknownUser")}</p>
                {session.userId ? (
                  <>
                    <p className="mt-3 text-sm text-slate-400">{t("home.id")}</p>
                    <p className="text-xs text-slate-300">{session.userId}</p>
                  </>
                ) : null}
                {session.email ? (
                  <>
                    <p className="mt-3 text-sm text-slate-400">{t("home.email")}</p>
                    <p className="text-sm text-slate-200">{session.email}</p>
                  </>
                ) : null}
                {session.roles.length ? (
                  <>
                    <p className="mt-3 text-sm text-slate-400">{t("home.roles")}</p>
                    <p className="text-xs text-slate-300">{session.roles.join(", ")}</p>
                  </>
                ) : null}
                {session.permissions.length ? (
                  <>
                    <p className="mt-3 text-sm text-slate-400">{t("home.permissions")}</p>
                    <p className="text-xs text-slate-300">{session.permissions.join(", ")}</p>
                  </>
                ) : null}
              </div>
              <div className="flex flex-wrap gap-3">
                <Link className="rounded-lg border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500" to="/account">
                  {t("common.account")}
                </Link>
                {isAdmin ? (
                  adminIsExternal ? (
                    <a className="rounded-lg bg-slate-800 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:bg-slate-700" href={adminUrl}>
                      {t("common.admin")}
                    </a>
                  ) : (
                    <Link className="rounded-lg bg-slate-800 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:bg-slate-700" to={adminUrl}>
                      {t("common.admin")}
                    </Link>
                  )
                ) : null}
              </div>
            </>
          ) : (
            <>
              <p>{t("home.anonymous")}</p>
              <div className="flex flex-wrap gap-3">
                <Link className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400" to="/login">
                  {t("login.submit")}
                </Link>
                <Link className="rounded-lg border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500" to="/register">
                  {t("common.register")}
                </Link>
              </div>
            </>
          )}
        </div>
      </Card>
    </div>
  );
};

export default Home;
