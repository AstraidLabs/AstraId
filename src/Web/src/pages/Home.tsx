import { useAuth } from "react-oidc-context";
import { useCallback, useEffect, useMemo, useState } from "react";
import { getPermissionClaims, hasPermission } from "../auth/permissions";

const Home = () => {
  const auth = useAuth();
  const [apiResponse, setApiResponse] = useState<string>("");
  const [adminResponse, setAdminResponse] = useState<string>("");
  const [permissions, setPermissions] = useState<string[]>([]);
  const [permissionsStatus, setPermissionsStatus] = useState<
    "idle" | "loading" | "loaded" | "error"
  >("idle");

  const handleLogin = () => {
    void auth.signinRedirect();
  };

  const handleLogout = () => {
    void auth.signoutRedirect();
  };

  const loadPermissions = useCallback(async () => {
    if (!auth.user) {
      setPermissions([]);
      setPermissionsStatus("idle");
      return;
    }

    const directPermissions = getPermissionClaims(auth.user);
    if (directPermissions.length > 0) {
      setPermissions(directPermissions);
      setPermissionsStatus("loaded");
      return;
    }

    if (!auth.user.access_token) {
      setPermissions([]);
      setPermissionsStatus("loaded");
      return;
    }

    setPermissionsStatus("loading");

    try {
      const response = await fetch("https://localhost:7002/api/me", {
        headers: {
          Authorization: `Bearer ${auth.user.access_token}`
        }
      });

      if (!response.ok) {
        setPermissionsStatus("error");
        return;
      }

      const data = (await response.json()) as { permissions?: string[] };
      setPermissions(Array.isArray(data.permissions) ? data.permissions : []);
      setPermissionsStatus("loaded");
    } catch (error) {
      setPermissionsStatus("error");
    }
  }, [auth.user]);

  useEffect(() => {
    void loadPermissions();
  }, [loadPermissions]);

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

    const data = (await response.json()) as { permissions?: string[] };
    setApiResponse(JSON.stringify(data, null, 2));
    if (Array.isArray(data.permissions)) {
      setPermissions(data.permissions);
      setPermissionsStatus("loaded");
    }
  };

  const adminPing = async () => {
    if (!auth.user?.access_token) {
      setAdminResponse("No access token available.");
      return;
    }

    const response = await fetch("https://localhost:7002/api/admin/ping", {
      headers: {
        Authorization: `Bearer ${auth.user.access_token}`
      }
    });

    if (!response.ok) {
      setAdminResponse(`Admin ping failed: ${response.status}`);
      return;
    }

    const data = await response.json();
    setAdminResponse(JSON.stringify(data, null, 2));
  };

  const canAdminPing = useMemo(
    () => hasPermission(auth.user, "system.admin", permissions),
    [auth.user, permissions]
  );

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
          {auth.isAuthenticated && canAdminPing ? (
            <button
              className="rounded border border-emerald-400 px-4 py-2 text-sm font-semibold text-emerald-200 hover:bg-emerald-500/10"
              onClick={adminPing}
            >
              Admin Ping
            </button>
          ) : null}
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
          <h2 className="text-lg font-semibold">User permissions</h2>
          {permissionsStatus === "loading" ? (
            <p className="mt-3 text-sm text-slate-400">Loading permissions…</p>
          ) : permissionsStatus === "error" ? (
            <p className="mt-3 text-sm text-rose-300">
              Unable to load permissions.
            </p>
          ) : permissions.length > 0 ? (
            <ul className="mt-3 space-y-1 text-sm text-emerald-200">
              {permissions.map((permission) => (
                <li key={permission}>{permission}</li>
              ))}
            </ul>
          ) : (
            <p className="mt-3 text-sm text-slate-400">
              {auth.isAuthenticated
                ? "No permissions found yet."
                : "Sign in to see permissions."}
            </p>
          )}
        </div>

        <div className="mt-6 rounded-lg border border-slate-800 bg-slate-900 p-4">
          <h2 className="text-lg font-semibold">/api/me response</h2>
          <pre className="mt-3 whitespace-pre-wrap text-sm text-sky-200">
            {apiResponse || "Call API to see response."}
          </pre>
        </div>

        <div className="mt-6 rounded-lg border border-slate-800 bg-slate-900 p-4">
          <h2 className="text-lg font-semibold">/api/admin/ping response</h2>
          <pre className="mt-3 whitespace-pre-wrap text-sm text-amber-200">
            {adminResponse || "Use Admin Ping to see response."}
          </pre>
        </div>
      </div>
    </div>
  );
};

export default Home;
