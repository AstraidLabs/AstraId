import { useEffect, useState } from "react";
import { apiRequest } from "../api/http";
import type { AdminSessionInfo } from "../api/types";
import { Link } from "react-router-dom";

export default function Dashboard() {
  const [session, setSession] = useState<AdminSessionInfo | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;
    const fetchSession = async () => {
      try {
        const data = await apiRequest<AdminSessionInfo>("/admin/api/me");
        if (isMounted) {
          setSession(data);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };
    fetchSession();
    return () => {
      isMounted = false;
    };
  }, []);

  return (
    <div className="flex flex-col gap-6">
      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
        <h1 className="text-2xl font-semibold text-white">Admin Dashboard</h1>
        <p className="mt-2 text-sm text-slate-300">
          Manage OAuth clients, scopes, users, and audit activity for the AuthServer.
        </p>
      </section>

      <section className="grid gap-4 md:grid-cols-2">
        <div className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
          <h2 className="text-lg font-semibold text-white">Session</h2>
          {loading && <p className="mt-3 text-sm text-slate-400">Loading session...</p>}
          {!loading && session && (
            <div className="mt-3 space-y-2 text-sm text-slate-300">
              <div>
                <span className="text-slate-400">Signed in as:</span>{" "}
                {session.email ?? session.userName ?? session.userId}
              </div>
              <div>
                <span className="text-slate-400">Roles:</span>{" "}
                {session.roles.length ? session.roles.join(", ") : "None"}
              </div>
              <div>
                <span className="text-slate-400">Permissions:</span>{" "}
                {session.permissions.length ? session.permissions.join(", ") : "None"}
              </div>
            </div>
          )}
        </div>

        <div className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
          <h2 className="text-lg font-semibold text-white">Quick links</h2>
          <div className="mt-3 flex flex-col gap-2 text-sm">
            <Link className="text-indigo-300 hover:text-indigo-200" to="/oidc/clients">
              Manage clients
            </Link>
            <Link className="text-indigo-300 hover:text-indigo-200" to="/oidc/scopes">
              Manage scopes
            </Link>
            <Link className="text-indigo-300 hover:text-indigo-200" to="/users">
              Review users
            </Link>
            <Link className="text-indigo-300 hover:text-indigo-200" to="/audit">
              Audit log
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
