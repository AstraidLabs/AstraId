import { useEffect, useState } from "react";
import { apiRequest } from "../api/http";
import type { AdminAuditListItem, PagedResult } from "../api/types";

export default function AuditList() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [result, setResult] = useState<PagedResult<AdminAuditListItem> | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    let isMounted = true;
    const fetchAudit = async () => {
      setLoading(true);
      const params = new URLSearchParams();
      if (search.trim()) {
        params.set("search", search.trim());
      }
      params.set("page", String(page));
      params.set("pageSize", String(pageSize));
      try {
        const data = await apiRequest<PagedResult<AdminAuditListItem>>(
          `/admin/api/audit?${params.toString()}`
        );
        if (isMounted) {
          setResult(data);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    fetchAudit();
    return () => {
      isMounted = false;
    };
  }, [page, pageSize, search]);

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / result.pageSize)) : 1;

  return (
    <section className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold text-white">Audit log</h1>
        <p className="text-sm text-slate-300">Review admin actions captured by the server.</p>
      </div>

      <div className="flex flex-wrap items-center gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <input
          className="w-full max-w-sm rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
          placeholder="Search action, target, or ID..."
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPage(1);
          }}
        />
        <select
          className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
          value={pageSize}
          onChange={(event) => {
            setPageSize(Number(event.target.value));
            setPage(1);
          }}
        >
          {[10, 20, 30].map((size) => (
            <option key={size} value={size}>
              {size} / page
            </option>
          ))}
        </select>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-800">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-900 text-slate-300">
            <tr>
              <th className="px-4 py-3 font-medium">Timestamp</th>
              <th className="px-4 py-3 font-medium">Action</th>
              <th className="px-4 py-3 font-medium">Target</th>
              <th className="px-4 py-3 font-medium">Actor</th>
              <th className="px-4 py-3 font-medium">Details</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800 bg-slate-950/40">
            {loading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-slate-400">
                  Loading audit entries...
                </td>
              </tr>
            )}
            {!loading && result?.items.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-slate-400">
                  No audit entries found.
                </td>
              </tr>
            )}
            {result?.items.map((entry) => (
              <tr key={entry.id} className="text-slate-100">
                <td className="px-4 py-3 text-slate-300">
                  {new Date(entry.timestampUtc).toLocaleString()}
                </td>
                <td className="px-4 py-3 font-medium">{entry.action}</td>
                <td className="px-4 py-3 text-slate-300">
                  {entry.targetType}
                  {entry.targetId ? ` (${entry.targetId})` : ""}
                </td>
                <td className="px-4 py-3 text-slate-300">
                  {entry.actorEmail ?? entry.actorUserId ?? "System"}
                </td>
                <td className="px-4 py-3 text-slate-400">
                  {entry.dataJson ? (
                    <code className="rounded bg-slate-900 px-2 py-1 text-xs text-slate-200">
                      {entry.dataJson}
                    </code>
                  ) : (
                    "-"
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {result && (
        <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-slate-300">
          <span>
            Page {result.page} of {totalPages} · {result.totalCount} entries
          </span>
          <div className="flex items-center gap-2">
            <button
              className="rounded-md border border-slate-700 px-3 py-1 text-slate-200 disabled:opacity-40"
              onClick={() => setPage((current) => Math.max(1, current - 1))}
              disabled={result.page <= 1}
            >
              Previous
            </button>
            <button
              className="rounded-md border border-slate-700 px-3 py-1 text-slate-200 disabled:opacity-40"
              onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
              disabled={result.page >= totalPages}
            >
              Next
            </button>
          </div>
        </div>
      )}
    </section>
  );
}
