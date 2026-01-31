import { useAuth } from "react-oidc-context";
import { useState } from "react";

const Home = () => {
  const auth = useAuth();
  const [apiResponse, setApiResponse] = useState<string>("");

  const handleLogin = () => {
    void auth.signinRedirect();
  };

  const handleLogout = () => {
    void auth.signoutRedirect();
  };

  const callApi = async () => {
    if (!auth.user?.access_token) {
      setApiResponse("No access token available.");
      return;
    }

    const response = await fetch("https://localhost:7002/api/me", {
      headers: {
        Authorization: `Bearer ${auth.user.access_token}`
      }
    });

    if (!response.ok) {
      setApiResponse(`API error: ${response.status}`);
      return;
    }

    const data = await response.json();
    setApiResponse(JSON.stringify(data, null, 2));
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <div className="mx-auto max-w-3xl px-6 py-12">
        <h1 className="text-3xl font-semibold">AstraId Demo SPA</h1>
        <p className="mt-2 text-slate-300">
          React SPA používající Authorization Code + PKCE přes OpenIddict.
        </p>

        <div className="mt-6 flex flex-wrap gap-3">
          {!auth.isAuthenticated ? (
            <button
              className="rounded bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400"
              onClick={handleLogin}
            >
              Login
            </button>
          ) : (
            <button
              className="rounded bg-slate-700 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-600"
              onClick={handleLogout}
            >
              Logout
            </button>
          )}
          <button
            className="rounded border border-indigo-400 px-4 py-2 text-sm font-semibold text-indigo-200 hover:bg-indigo-500/10"
            onClick={callApi}
            disabled={!auth.isAuthenticated}
          >
            Call API
          </button>
        </div>

        <div className="mt-8 rounded-lg border border-slate-800 bg-slate-900 p-4">
          <h2 className="text-lg font-semibold">User info</h2>
          {auth.isAuthenticated ? (
            <pre className="mt-3 whitespace-pre-wrap text-sm text-emerald-200">
              {JSON.stringify(auth.user?.profile, null, 2)}
            </pre>
          ) : (
            <p className="mt-3 text-sm text-slate-400">Not signed in.</p>
          )}
        </div>

        <div className="mt-6 rounded-lg border border-slate-800 bg-slate-900 p-4">
          <h2 className="text-lg font-semibold">/api/me response</h2>
          <pre className="mt-3 whitespace-pre-wrap text-sm text-sky-200">
            {apiResponse || "Call API to see response."}
          </pre>
        </div>
      </div>
    </div>
  );
};

export default Home;
