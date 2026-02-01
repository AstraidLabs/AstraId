import { useState } from "react";
import { useAuth } from "react-oidc-context";
import Card from "../components/Card";
import Alert from "../components/Alert";
import { ApiError } from "../api/http";
import {
  getAuthServerIntegrationPing,
  getCmsIntegrationPing
} from "../api/endpoints";
import { usePermissions } from "../auth/usePermissions";

const Integrations = () => {
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
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError("Integrace nejsou dostupné.");
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <Card
        title="Integrace"
        description="Kontrola dostupnosti napojených služeb."
      >
        {!auth.isAuthenticated ? (
          <Alert variant="warning">
            Přihlaste se pro kontrolu integrací.
          </Alert>
        ) : null}
        {auth.isAuthenticated && !hasPermission("system.admin") ? (
          <Alert variant="error">
            Integrace jsou dostupné pouze s oprávněním system.admin.
          </Alert>
        ) : null}

        {auth.isAuthenticated && hasPermission("system.admin") ? (
          <div className="flex flex-wrap gap-3">
            <button
              className="rounded-full border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500 disabled:cursor-not-allowed disabled:opacity-60"
              onClick={() =>
                token
                  ? runCheck(() => getAuthServerIntegrationPing(token))
                  : setError("Chybí access token.")
              }
              disabled={loading}
            >
              AuthServer ping
            </button>
            <button
              className="rounded-full border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500 disabled:cursor-not-allowed disabled:opacity-60"
              onClick={() =>
                token
                  ? runCheck(() => getCmsIntegrationPing(token))
                  : setError("Chybí access token.")
              }
              disabled={loading}
            >
              CMS ping
            </button>
          </div>
        ) : null}

        {error ? <Alert variant="error">{error}</Alert> : null}
        {result ? (
          <pre className="mt-4 rounded-xl border border-slate-800 bg-slate-950/60 p-4 text-sm text-emerald-200">
            {result}
          </pre>
        ) : null}
      </Card>
    </div>
  );
};

export default Integrations;
