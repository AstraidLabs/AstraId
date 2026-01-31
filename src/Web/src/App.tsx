import { useState } from "react";
import { useAuth } from "react-oidc-context";

const App = () => {
  const auth = useAuth();
  const [apiResponse, setApiResponse] = useState<string>("");
  const [apiError, setApiError] = useState<string>("");
  const [apiLoading, setApiLoading] = useState(false);

  const handleLogin = () => {
    void auth.signinRedirect();
  };

  const handleLogout = () => {
    void auth.signoutRedirect();
  };

  const callApi = async () => {
    setApiError("");
    setApiResponse("");

    if (!auth.user?.access_token) {
      setApiError("Missing access token. Please sign in first.");
      return;
    }

    setApiLoading(true);

    try {
      const response = await fetch("https://localhost:7002/api/me", {
        headers: {
          Authorization: `Bearer ${auth.user.access_token}`
        }
      });

      if (!response.ok) {
        if (response.status === 401 || response.status === 403) {
          setApiError("Not authorized. Please sign in again.");
        } else {
          setApiError(`API error: ${response.status}`);
        }
        return;
      }

      const data = await response.json();
      setApiResponse(JSON.stringify(data, null, 2));
    } catch (error) {
      setApiError("API request failed. Check the backend/API availability.");
    } finally {
      setApiLoading(false);
    }
  };

  const profile = auth.user?.profile;
  const isAuthenticated = auth.isAuthenticated;

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <div className="mx-auto flex max-w-3xl flex-col gap-6 px-6 py-12">
        <header>
          <p className="text-sm uppercase tracking-[0.2em] text-slate-400">
            AstraId SPA
          </p>
          <h1 className="mt-2 text-3xl font-semibold">
            React + OIDC + Tailwind
          </h1>
          <p className="mt-3 text-slate-300">
            Připraveno pro přihlášení přes AuthServer (OIDC code flow).
          </p>
        </header>

        <section className="rounded-2xl border border-slate-800 bg-slate-900/70 p-6 shadow-xl shadow-slate-950/50">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div>
              <h2 className="text-lg font-semibold">Přihlášení</h2>
              <p className="text-sm text-slate-400">
                Stav:{" "}
                <span className="font-medium text-slate-200">
                  {isAuthenticated ? "Přihlášen" : "Nepřihlášen"}
                </span>
              </p>
            </div>
            <div className="flex flex-wrap gap-3">
              {!isAuthenticated ? (
                <button
                  className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400"
                  onClick={handleLogin}
                >
                  Login
                </button>
              ) : (
                <button
                  className="rounded-lg bg-slate-700 px-4 py-2 text-sm font-semibold text-white transition hover:bg-slate-600"
                  onClick={handleLogout}
                >
                  Logout
                </button>
              )}
              <button
                className="rounded-lg border border-indigo-400 px-4 py-2 text-sm font-semibold text-indigo-200 transition hover:bg-indigo-500/10 disabled:cursor-not-allowed disabled:opacity-50"
                onClick={callApi}
                disabled={!isAuthenticated || apiLoading}
              >
                {apiLoading ? "Calling..." : "Call API"}
              </button>
            </div>
          </div>
        </section>

        <section className="rounded-2xl border border-slate-800 bg-slate-900/70 p-6">
          <h2 className="text-lg font-semibold">Token info</h2>
          {isAuthenticated ? (
            <dl className="mt-4 grid gap-4 text-sm sm:grid-cols-3">
              <div className="rounded-lg bg-slate-950/60 p-3">
                <dt className="text-slate-400">sub</dt>
                <dd className="mt-1 text-slate-100">
                  {profile?.sub ?? "—"}
                </dd>
              </div>
              <div className="rounded-lg bg-slate-950/60 p-3">
                <dt className="text-slate-400">name</dt>
                <dd className="mt-1 text-slate-100">
                  {profile?.name ?? "—"}
                </dd>
              </div>
              <div className="rounded-lg bg-slate-950/60 p-3">
                <dt className="text-slate-400">email</dt>
                <dd className="mt-1 text-slate-100">
                  {profile?.email ?? "—"}
                </dd>
              </div>
            </dl>
          ) : (
            <p className="mt-3 text-sm text-slate-400">
              Přihlaš se pro zobrazení tokenu.
            </p>
          )}
        </section>

        <section className="rounded-2xl border border-slate-800 bg-slate-900/70 p-6">
          <h2 className="text-lg font-semibold">API</h2>
          <p className="mt-2 text-sm text-slate-400">
            Volá <span className="text-slate-200">GET</span>{" "}
            <span className="text-slate-200">https://localhost:7002/api/me</span>
          </p>
          {apiError ? (
            <p className="mt-4 rounded-lg border border-rose-500/30 bg-rose-500/10 p-3 text-sm text-rose-200">
              {apiError}
            </p>
          ) : null}
          <pre className="mt-4 max-h-64 overflow-auto rounded-lg bg-slate-950/60 p-4 text-sm text-sky-200">
            {apiResponse || "Zatím bez odpovědi."}
          </pre>
        </section>
      </div>
    </div>
  );
};

export default App;
