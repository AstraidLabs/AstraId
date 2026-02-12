import { useEffect, useMemo, useState } from "react";
import { useAuth } from "react-oidc-context";
import Card from "../components/Card";
import Alert from "../components/Alert";
import { AppError } from "../api/http";
import { getMe, type MeResponse } from "../api/endpoints";
import { usePermissions } from "../auth/usePermissions";
import { useLanguage } from "../i18n/LanguageProvider";

const Profile = () => {
  const { t } = useLanguage();
  const auth = useAuth();
  const [me, setMe] = useState<MeResponse | null>(null);
  const [error, setError] = useState<string>("");
  const [loading, setLoading] = useState(false);

  const token = auth.user?.access_token;
  const fallbackPermissions = useMemo(() => me?.permissions ?? [], [me?.permissions]);
  const { permissions } = usePermissions(fallbackPermissions);

  useEffect(() => {
    if (!token) {
      setMe(null);
      return;
    }

    let mounted = true;
    const load = async () => {
      setLoading(true);
      setError("");
      try {
        const data = await getMe(token);
        if (mounted) setMe(data);
      } catch (err) {
        if (mounted) {
          if (err instanceof AppError) setError(err.detail ?? err.message);
          else setError(t("profile.error"));
        }
      } finally {
        if (mounted) setLoading(false);
      }
    };

    void load();
    return () => {
      mounted = false;
    };
  }, [token, t]);

  const profile = auth.user?.profile;

  return (
    <div className="flex flex-col gap-6">
      <Card title={t("profile.title")} description={t("profile.description")}>
        {!auth.isAuthenticated ? <Alert variant="warning">{t("profile.notSignedIn")}</Alert> : null}
        {error ? <Alert variant="error">{error}</Alert> : null}

        <dl className="mt-4 grid gap-4 text-sm text-slate-200 sm:grid-cols-3">
          <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
            <dt className="text-slate-400">sub</dt>
            <dd className="mt-1 text-white">{profile?.sub ?? "—"}</dd>
          </div>
          <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
            <dt className="text-slate-400">name</dt>
            <dd className="mt-1 text-white">{profile?.name ?? me?.name ?? "—"}</dd>
          </div>
          <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
            <dt className="text-slate-400">email</dt>
            <dd className="mt-1 text-white">{profile?.email ?? me?.email ?? "—"}</dd>
          </div>
        </dl>

        <div className="mt-6">
          <h3 className="text-sm font-semibold text-slate-300">{t("profile.permissions")}</h3>
          <div className="mt-3 flex flex-wrap gap-2">
            {permissions.length === 0 ? (
              <span className="text-sm text-slate-500">{t("profile.permissionsEmpty")}</span>
            ) : (
              permissions.map((permission) => (
                <span key={permission} className="rounded-full border border-indigo-400/40 bg-indigo-500/10 px-3 py-1 text-xs text-indigo-100">
                  {permission}
                </span>
              ))
            )}
          </div>
        </div>

        <p className="mt-6 text-sm text-slate-400">{loading ? t("profile.loading") : ""}</p>
      </Card>
    </div>
  );
};

export default Profile;
