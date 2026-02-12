import { useState } from "react";
import { useAuth } from "react-oidc-context";
import Card from "../components/Card";
import Alert from "../components/Alert";
import { AppError } from "../api/http";
import { getAuthServerIntegrationPing, getCmsIntegrationPing } from "../api/endpoints";
import { usePermissions } from "../auth/usePermissions";
import { useLanguage } from "../i18n/LanguageProvider";

const Integrations = () => {
  const { t } = useLanguage();
  const auth = useAuth();
  const { hasPermission } = usePermissions();
  const [result, setResult] = useState<string>("");
  const [error, setError] = useState<string>("");
  const [loading, setLoading] = useState(false);

  const token = auth.user?.access_token;

  const runCheck = async (action: () => Promise<unknown>) => {
    setLoading(true);
    setError("");
    setResult("");

    try {
      const data = await action();
      setResult(JSON.stringify(data, null, 2));
    } catch (err) {
      if (err instanceof AppError) {
        setError(err.detail ?? err.message);
      } else {
        setError(t("integrations.unavailable"));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <Card title={t("integrations.title")} description={t("integrations.description")}>
        {!auth.isAuthenticated ? <Alert variant="warning">{t("integrations.signInRequired")}</Alert> : null}
        {auth.isAuthenticated && !hasPermission("system.admin") ? <Alert variant="error">{t("integrations.adminPermissionRequired")}</Alert> : null}

        {auth.isAuthenticated && hasPermission("system.admin") ? (
          <div className="flex flex-wrap gap-3">
            <button
              className="rounded-full border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500 disabled:cursor-not-allowed disabled:opacity-60"
              onClick={() => (token ? runCheck(() => getAuthServerIntegrationPing(token)) : setError(t("integrations.missingToken")))}
              disabled={loading}
            >
              {t("integrations.authServerPing")}
            </button>
            <button
              className="rounded-full border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500 disabled:cursor-not-allowed disabled:opacity-60"
              onClick={() => (token ? runCheck(() => getCmsIntegrationPing(token)) : setError(t("integrations.missingToken")))}
              disabled={loading}
            >
              {t("integrations.cmsPing")}
            </button>
          </div>
        ) : null}

        {error ? <Alert variant="error">{error}</Alert> : null}
        {result ? <pre className="mt-4 rounded-xl border border-slate-800 bg-slate-950/60 p-4 text-sm text-emerald-200">{result}</pre> : null}
      </Card>
    </div>
  );
};

export default Integrations;
