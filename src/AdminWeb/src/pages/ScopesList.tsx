import { useEffect, useState } from "react";
import { apiRequest } from "../api/http";
import type { AdminClientScopeItem } from "../api/types";

export default function ScopesList() {
  const [scopes, setScopes] = useState<AdminClientScopeItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;
    const fetchScopes = async () => {
      try {
        const data = await apiRequest<AdminClientScopeItem[]>("/admin/api/scopes");
        if (isMounted) {
          setScopes(data);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };
    fetchScopes();
    return () => {
      isMounted = false;
    };
  }, []);

  return (
    <section className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold text-white">Scopes</h1>
        <p className="text-sm text-slate-300">Available API scopes registered in OpenIddict.</p>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-800">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-900 text-slate-300">
            <tr>
              <th className="px-4 py-3 font-medium">Name</th>
              <th className="px-4 py-3 font-medium">Display name</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800 bg-slate-950/40">
            {loading && (
              <tr>
                <td colSpan={2} className="px-4 py-6 text-center text-slate-400">
                  Loading scopes...
                </td>
              </tr>
            )}
            {!loading && scopes.length === 0 && (
              <tr>
                <td colSpan={2} className="px-4 py-6 text-center text-slate-400">
                  No scopes available.
                </td>
              </tr>
            )}
            {scopes.map((scope) => (
              <tr key={scope.name} className="text-slate-100">
                <td className="px-4 py-3 font-medium">{scope.name}</td>
                <td className="px-4 py-3 text-slate-300">{scope.displayName ?? "-"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
