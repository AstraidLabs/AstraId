import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiRequest } from "../api/http";
import type { AdminSessionInfo } from "../api/types";
import { toAdminRoute } from "../../routing";
import { ADMIN_CATEGORIES } from "../adminNavigation";

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
          Manage directory, applications, security controls, integrations and governance from one place.
        </p>
      </section>

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {ADMIN_CATEGORIES.map((category) => {
          const Icon = category.icon;

          return (
            <Link
              key={category.id}
              to={toAdminRoute(category.path)}
              className="rounded-xl border border-slate-800 bg-slate-900/30 p-5 transition hover:border-indigo-500/60 hover:bg-slate-900/60 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-400"
            >
              <div className="flex items-center gap-3">
                <span className="rounded-lg bg-slate-800/80 p-2 text-indigo-300">
                  <Icon className="h-5 w-5" aria-hidden="true" />
                </span>
                <h2 className="text-base font-semibold text-white">{category.label}</h2>
              </div>
              <p className="mt-3 text-sm text-slate-400">{category.description}</p>
            </Link>
          );
        })}
      </section>

      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
        <h2 className="text-lg font-semibold text-white">Session</h2>
        {loading && <p className="mt-3 text-sm text-slate-400">Loading session...</p>}
        {!loading && session && (
          <div className="mt-3 space-y-2 text-sm text-slate-300">
            <div>
              <span className="text-slate-400">Signed in as:</span> {session.email ?? session.userName ?? session.userId}
            </div>
            <div>
              <span className="text-slate-400">Roles:</span> {session.roles.length ? session.roles.join(", ") : "None"}
            </div>
            <div>
              <span className="text-slate-400">Permissions:</span> {session.permissions.length ? session.permissions.join(", ") : "None"}
            </div>
          </div>
        )}
      </section>
    </div>
  );
}
