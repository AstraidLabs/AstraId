import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import Card from "../components/Card";
import Alert from "../components/Alert";
import { logout } from "../api/authServer";
import { useAuthSession } from "../auth/useAuthSession";

const Home = () => {
  const navigate = useNavigate();
  const { session, isLoading, error, refresh } = useAuthSession();
  const [logoutError, setLogoutError] = useState("");
  const adminUrl = "https://localhost:7001/admin";

  const isAdmin = useMemo(() => {
    if (!session?.permissions?.length) {
      return false;
    }
    return session.permissions.some(
      (permission) => permission.toLowerCase() === "system.admin"
    );
  }, [session]);

  const handleLogout = async () => {
    setLogoutError("");
    try {
      await logout();
      await refresh();
      navigate("/", { replace: true });
    } catch (err) {
      setLogoutError(
        err instanceof Error ? err.message : "Nepodařilo se odhlásit."
      );
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <Card
        title="Public UI"
        description="Veřejné přihlašovací rozhraní pro uživatele."
      >
        <div className="flex flex-col gap-3 text-sm text-slate-300">
          {isLoading ? (
            <Alert variant="info">Načítání session...</Alert>
          ) : error ? (
            <Alert variant="warning">{error}</Alert>
          ) : session && session.isAuthenticated ? (
            <>
              <p className="text-base font-semibold text-white">
                Úspěšně přihlášen/a, zatím tu nic není.
              </p>
              <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
                <p className="text-sm text-slate-400">Uživatel</p>
                <p className="text-sm text-white">
                  {session.userName ?? session.email ?? "Neznámý uživatel"}
                </p>
                {session.userId ? (
                  <>
                    <p className="mt-3 text-sm text-slate-400">ID</p>
                    <p className="text-xs text-slate-300">{session.userId}</p>
                  </>
                ) : null}
                {session.email ? (
                  <>
                    <p className="mt-3 text-sm text-slate-400">E-mail</p>
                    <p className="text-sm text-slate-200">{session.email}</p>
                  </>
                ) : null}
                {session.roles.length ? (
                  <>
                    <p className="mt-3 text-sm text-slate-400">Role</p>
                    <p className="text-xs text-slate-300">
                      {session.roles.join(", ")}
                    </p>
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
                {isAdmin ? (
                  <a
                    className="rounded-lg bg-slate-800 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:bg-slate-700"
                    href={adminUrl}
                  >
                    Admin
                  </a>
                ) : null}
              </div>
            </>
          ) : (
            <>
              <p>Nejste přihlášeni. Pokračujte přihlášením nebo registrací.</p>
              <div className="flex flex-wrap gap-3">
                <Link
                  className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400"
                  to="/login"
                >
                  Přihlásit se
                </Link>
                <Link
                  className="rounded-lg border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500"
                  to="/register"
                >
                  Registrovat
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
