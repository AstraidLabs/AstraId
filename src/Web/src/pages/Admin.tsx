import { useState } from "react";
import { useAuth } from "react-oidc-context";
import Card from "../components/Card";
import Alert from "../components/Alert";
import { ApiError } from "../api/http";
import {
  getAdminPing,
  getAuthServerIntegrationPing,
  getCmsIntegrationPing
} from "../api/endpoints";
import { usePermissions } from "../auth/usePermissions";

const Admin = () => {
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
        setError("Administrativní API není dostupné.");
      }
    } finally {
      setLoading(false);
    }
  };

  if (!auth.isAuthenticated) {
    return (
      <Card title="Administrace">
        <Alert variant="warning">
          Nejprve se přihlaste, aby bylo možné zobrazit administrativní data.
        </Alert>
      </Card>
    );
  }

  if (!hasPermission("system.admin")) {
    return (
      <Card title="Administrace">
        <Alert variant="error">
          Tento obsah je dostupný pouze pro uživatele s oprávněním
          <strong> system.admin</strong>.
        </Alert>
      </Card>
    );
  }

  if (!token) {
    return (
      <Card title="Administrace">
        <Alert variant="warning">
          Chybí access token. Přihlaste se znovu.
        </Alert>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <Card
        title="Admin ping"
        description="Volá /api/admin/ping a ověřuje oprávnění system.admin."
      >
        <div className="flex flex-wrap gap-3">
          <button
            className="rounded-full bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60"
            onClick={() => runCheck(() => getAdminPing(token))}
            disabled={loading}
          >
            {loading ? "Načítám..." : "Ověřit admin ping"}
          </button>
        </div>
        {error ? <Alert variant="error">{error}</Alert> : null}
        {result ? (
          <pre className="mt-4 rounded-xl border border-slate-800 bg-slate-950/60 p-4 text-sm text-emerald-200">
            {result}
          </pre>
        ) : null}
      </Card>

      <Card
        title="Integrace"
        description="Testuje volání interních služeb přes API."
      >
        <div className="flex flex-wrap gap-3">
          <button
            className="rounded-full border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500 disabled:cursor-not-allowed disabled:opacity-60"
            onClick={() => runCheck(() => getAuthServerIntegrationPing(token))}
            disabled={loading}
          >
            AuthServer ping
          </button>
          <button
            className="rounded-full border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500 disabled:cursor-not-allowed disabled:opacity-60"
            onClick={() => runCheck(() => getCmsIntegrationPing(token))}
            disabled={loading}
          >
            CMS ping
          </button>
        </div>
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

export default Admin;
