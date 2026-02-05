import { useEffect, useState } from "react";
import { apiRequest } from "../api/http";
import type { AdminTokenIncidentDetail, AdminTokenIncidentListItem, PagedResult } from "../api/types";

const severityTone: Record<string, string> = {
  low: "bg-slate-700/40 text-slate-200",
  medium: "bg-indigo-500/20 text-indigo-200",
  high: "bg-rose-500/20 text-rose-200",
  critical: "bg-rose-500/40 text-rose-100",
};

const formatDate = (value?: string | null) => (value ? new Date(value).toLocaleString() : "—");

export default function SecurityIncidents() {
  const [items, setItems] = useState<AdminTokenIncidentListItem[]>([]);
  const [selected, setSelected] = useState<AdminTokenIncidentDetail | null>(null);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      const response = await apiRequest<PagedResult<AdminTokenIncidentListItem>>(
        "/admin/api/security/token-incidents?page=1&pageSize=50"
      );
      setItems(response.items);
    } finally {
      setLoading(false);
    }
  };

  const loadDetail = async (id: string) => {
    const response = await apiRequest<AdminTokenIncidentDetail>(
      `/admin/api/security/token-incidents/${id}`
    );
    setSelected(response);
  };

  useEffect(() => {
    load();
  }, []);

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">Incidents</h1>
        <p className="mt-1 text-sm text-slate-400">
          Review security incidents such as refresh token reuse or key events.
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-[2fr_1fr]">
        <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-5">
          <h2 className="text-lg font-semibold text-white">Recent incidents</h2>
          <div className="mt-4 overflow-hidden rounded-lg border border-slate-800">
            <table className="min-w-full divide-y divide-slate-800 text-sm">
              <thead className="bg-slate-950">
                <tr className="text-left text-xs uppercase tracking-wide text-slate-500">
                  <th className="px-4 py-3 font-semibold">Timestamp</th>
                  <th className="px-4 py-3 font-semibold">Type</th>
                  <th className="px-4 py-3 font-semibold">Severity</th>
                  <th className="px-4 py-3 font-semibold">Client</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800 text-slate-200">
                {loading && (
                  <tr>
                    <td className="px-4 py-4 text-sm text-slate-400" colSpan={4}>
                      Loading incidents…
                    </td>
                  </tr>
                )}
                {!loading && items.length === 0 && (
                  <tr>
                    <td className="px-4 py-4 text-sm text-slate-400" colSpan={4}>
                      No incidents recorded.
                    </td>
                  </tr>
                )}
                {items.map((incident) => (
                  <tr
                    key={incident.id}
                    className="cursor-pointer hover:bg-slate-900/40"
                    onClick={() => loadDetail(incident.id)}
                  >
                    <td className="px-4 py-3">{formatDate(incident.timestampUtc)}</td>
                    <td className="px-4 py-3">{incident.type}</td>
                    <td className="px-4 py-3">
                      <span
                        className={`rounded-full px-2 py-1 text-xs font-semibold ${
                          severityTone[incident.severity] ?? "bg-slate-700/40 text-slate-200"
                        }`}
                      >
                        {incident.severity}
                      </span>
                    </td>
                    <td className="px-4 py-3">{incident.clientId ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-5">
          <h2 className="text-lg font-semibold text-white">Incident detail</h2>
          {selected ? (
            <div className="mt-4 text-sm text-slate-200">
              <div className="space-y-2">
                <div>
                  <div className="text-xs uppercase tracking-wide text-slate-500">Type</div>
                  <div>{selected.type}</div>
                </div>
                <div>
                  <div className="text-xs uppercase tracking-wide text-slate-500">Severity</div>
                  <div>{selected.severity}</div>
                </div>
                <div>
                  <div className="text-xs uppercase tracking-wide text-slate-500">Timestamp</div>
                  <div>{formatDate(selected.timestampUtc)}</div>
                </div>
                <div>
                  <div className="text-xs uppercase tracking-wide text-slate-500">Client</div>
                  <div>{selected.clientId ?? "—"}</div>
                </div>
                <div>
                  <div className="text-xs uppercase tracking-wide text-slate-500">User</div>
                  <div>{selected.userId ?? "—"}</div>
                </div>
                <div>
                  <div className="text-xs uppercase tracking-wide text-slate-500">Trace</div>
                  <div>{selected.traceId ?? "—"}</div>
                </div>
              </div>
              <div className="mt-4">
                <div className="text-xs uppercase tracking-wide text-slate-500">Detail</div>
                <pre className="mt-2 max-h-64 overflow-auto rounded-md border border-slate-800 bg-slate-950 p-3 text-xs text-slate-200">
                  {selected.detailJson ?? "{}"}
                </pre>
              </div>
            </div>
          ) : (
            <p className="mt-4 text-sm text-slate-400">Select an incident to view details.</p>
          )}
        </div>
      </div>
    </div>
  );
}
