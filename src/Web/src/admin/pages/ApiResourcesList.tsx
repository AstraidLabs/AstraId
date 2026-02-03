import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiRequest } from "../api/http";
import type { AdminApiResourceListItem } from "../api/types";
import { toAdminRoute } from "../../routing";

export default function ApiResourcesList() {
  const [resources, setResources] = useState<AdminApiResourceListItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;
    const fetchResources = async () => {
      try {
        const data = await apiRequest<AdminApiResourceListItem[]>("/admin/api/api-resources");
        if (isMounted) {
          setResources(data);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };
    fetchResources();
    return () => {
      isMounted = false;
    };
  }, []);

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-white">API Endpoints</h1>
          <p className="text-sm text-slate-300">Manage API resources and endpoint permissions.</p>
        </div>
        <Link
          to={toAdminRoute("/config/api-resources/new")}
          className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400"
        >
          New API resource
        </Link>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-800">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-900 text-slate-300">
            <tr>
              <th className="px-4 py-3 font-medium">Name</th>
              <th className="px-4 py-3 font-medium">Display name</th>
              <th className="px-4 py-3 font-medium">Base URL</th>
              <th className="px-4 py-3 font-medium">Status</th>
              <th className="px-4 py-3 text-right font-medium">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800 bg-slate-950/40">
            {loading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-slate-400">
                  Loading API resources...
                </td>
              </tr>
            )}
            {!loading && resources.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-slate-400">
                  No API resources defined.
                </td>
              </tr>
            )}
            {resources.map((resource) => (
              <tr key={resource.id} className="text-slate-100">
                <td className="px-4 py-3 font-medium">{resource.name}</td>
                <td className="px-4 py-3 text-slate-300">{resource.displayName}</td>
                <td className="px-4 py-3 text-slate-300">{resource.baseUrl ?? "-"}</td>
                <td className="px-4 py-3">
                  <span
                    className={`rounded-full px-3 py-1 text-xs font-semibold ${
                      resource.isActive
                        ? "bg-emerald-500/20 text-emerald-200"
                        : "bg-rose-500/20 text-rose-200"
                    }`}
                  >
                    {resource.isActive ? "Active" : "Inactive"}
                  </span>
                </td>
                <td className="px-4 py-3 text-right">
                  <div className="flex items-center justify-end gap-3">
                    <Link
                      to={toAdminRoute(`/config/api-resources/${resource.id}`)}
                      className="text-sm font-semibold text-indigo-300 hover:text-indigo-200"
                    >
                      Edit
                    </Link>
                    <Link
                      to={toAdminRoute(`/config/api-resources/${resource.id}/endpoints`)}
                      className="text-sm font-semibold text-slate-300 hover:text-slate-100"
                    >
                      Endpoints
                    </Link>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
