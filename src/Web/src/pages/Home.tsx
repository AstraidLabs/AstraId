import { useState } from "react";
import { useAuth } from "react-oidc-context";
import { Link } from "react-router-dom";
import Card from "../components/Card";
import Alert from "../components/Alert";
import { logout } from "../api/authServer";
import { useAuthSession } from "../auth/useAuthSession";

const Home = () => {
  const auth = useAuth();
  const { session, isLoading, error, refresh } = useAuthSession();
  const [logoutError, setLogoutError] = useState("");

  const handleLogout = async () => {
    setLogoutError("");
    try {
      await logout();
      await refresh();
    } catch (err) {
      setLogoutError(
        err instanceof Error ? err.message : "Nepodařilo se odhlásit."
      );
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <Card
        title="Standalone AuthServer"
        description="AuthServer běží samostatně i bez připojené klientské aplikace."
      >
        <div className="flex flex-col gap-3 text-sm text-slate-300">
          {isLoading ? (
            <Alert variant="info">Načítání session...</Alert>
          ) : error ? (
            <Alert variant="warning">{error}</Alert>
          ) : session && session.isAuthenticated ? (
            <>
              <p className="text-base font-semibold text-white">
                Přihlášení úspěšné.
              </p>
              <p>Zatím tu nic není – připojte klientskou aplikaci.</p>
              <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
                <p className="text-sm text-slate-400">Uživatel</p>
                <p className="text-sm text-white">
                  {session.userName ?? session.email ?? "Neznámý uživatel"}
                </p>
                <p className="mt-3 text-sm text-slate-400">ID</p>
                <p className="text-xs text-slate-300">{session.userId}</p>
                {session.email ? (
                  <>
                    <p className="mt-3 text-sm text-slate-400">E-mail</p>
                    <p className="text-sm text-slate-200">{session.email}</p>
                  </>
                ) : null}
                {session.permissions.length ? (
                  <>
                    <p className="mt-3 text-sm text-slate-400">Oprávnění</p>
                    <p className="text-xs text-slate-300">
                      {session.permissions.join(", ")}
                    </p>
                  </>
                ) : null}
              </div>
              {logoutError ? (
                <Alert variant="warning">{logoutError}</Alert>
              ) : null}
              <div className="flex flex-wrap gap-3">
                <button
                  className="rounded-lg border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500"
                  onClick={handleLogout}
                >
                  Logout
                </button>
                {session.permissions.includes("system.admin") ? (
                  <a
                    className="rounded-lg bg-slate-800 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:bg-slate-700"
                    href="/admin"
                  >
                    Admin
                  </a>
                ) : null}
              </div>
            </>
          ) : (
            <>
              <p>Nejste přihlášeni. Přihlaste se nebo se zaregistrujte.</p>
              <div className="flex flex-wrap gap-3">
                <Link
                  className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400"
                  to="/login"
                >
                  Login
                </Link>
                <Link
                  className="rounded-lg border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500"
                  to="/register"
                >
                  Register
                </Link>
              </div>
            </>
          )}
        </div>
      </Card>

      <Card
        title="OIDC připojení"
        description="Authorization Code + PKCE pro SPA klienta."
      >
        <p className="text-sm text-slate-300">
          Kliknutím spustíte OIDC flow. Pokud nemáte cookie session, budete
          přesměrováni na login.
        </p>
        <button
          className="mt-4 rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400"
          onClick={() => void auth.signinRedirect()}
        >
          Připojit aplikaci (OIDC)
        </button>
      </Card>
    </div>
  );
};

export default Home;
