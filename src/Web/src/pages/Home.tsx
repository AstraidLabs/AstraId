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
        err instanceof Error ? err.message : "Unable to sign out."
      );
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <Card
        title="Public UI"
        description="Public sign-in interface for users."
      >
        <div className="flex flex-col gap-3 text-sm text-slate-300">
          {isLoading ? (
            <Alert variant="info">Loading session...</Alert>
          ) : error ? (
            <Alert variant="warning">{error}</Alert>
          ) : session && session.isAuthenticated ? (
            <>
              <p className="text-base font-semibold text-white">
                You are signed in. Nothing else to show here yet.
              </p>
              <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
                <p className="text-sm text-slate-400">User</p>
                <p className="text-sm text-white">
                  {session.userName ?? session.email ?? "Unknown user"}
                </p>
                {session.userId ? (
                  <>
                    <p className="mt-3 text-sm text-slate-400">ID</p>
                    <p className="text-xs text-slate-300">{session.userId}</p>
                  </>
                ) : null}
                {session.email ? (
                  <>
                    <p className="mt-3 text-sm text-slate-400">Email</p>
                    <p className="text-sm text-slate-200">{session.email}</p>
                  </>
                ) : null}
                {session.roles.length ? (
                  <>
                    <p className="mt-3 text-sm text-slate-400">Roles</p>
                    <p className="text-xs text-slate-300">
                      {session.roles.join(", ")}
                    </p>
                  </>
                ) : null}
                {session.permissions.length ? (
                  <>
                    <p className="mt-3 text-sm text-slate-400">Permissions</p>
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
              <p>You are not signed in. Continue by signing in or registering.</p>
              <div className="flex flex-wrap gap-3">
                <Link
                  className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400"
                  to="/login"
                >
                  Sign in
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
    </div>
  );
};

export default Home;
