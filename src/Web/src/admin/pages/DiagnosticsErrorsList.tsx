import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { AppError, apiRequest } from "../api/http";
import type { AdminErrorLogListItem, PagedResult } from "../api/types";
import { Field, FormError } from "../components/Field";
import { toAdminRoute } from "../../routing";
import { validateAuditDateRange } from "../validation/adminValidation";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

export default function DiagnosticsErrorsList() {
  const [traceId, setTraceId] = useState("");
  const [status, setStatus] = useState("");
  const [fromUtc, setFromUtc] = useState("");
  const [toUtc, setToUtc] = useState("");
  const [filterError, setFilterError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [formDiagnostics, setFormDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [result, setResult] = useState<PagedResult<AdminErrorLogListItem> | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const rangeError = validateAuditDateRange(fromUtc, toUtc);
    setFilterError(rangeError);
    if (rangeError) {
      return;
    }

    let isMounted = true;
    const fetchErrors = async () => {
      setLoading(true);
      const params = new URLSearchParams();
      if (traceId.trim()) {
        params.set("traceId", traceId.trim());
      }
      if (status.trim()) {
        params.set("status", status.trim());
      }
      if (fromUtc) {
        params.set("fromUtc", new Date(fromUtc).toISOString());
      }
      if (toUtc) {
        params.set("toUtc", new Date(toUtc).toISOString());
      }
      params.set("page", String(page));
      params.set("pageSize", String(pageSize));

      try {
        const data = await apiRequest<PagedResult<AdminErrorLogListItem>>(
          `/admin/api/diagnostics/errors?${params.toString()}`,
          { suppressToast: true }
        );
        if (isMounted) {
          setResult(data);
          setFormError(null);
          setFormDiagnostics(undefined);
        }
      } catch (error) {
        if (error instanceof AppError) {
          const parsed = parseProblemDetailsErrors(error);
          if (isMounted) {
            setFormError(parsed.generalError ?? "Unable to load error logs.");
            setFormDiagnostics(parsed.diagnostics);
          }
        } else if (isMounted) {
          setFormError("Unable to load error logs.");
          setFormDiagnostics(undefined);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    fetchErrors();
    return () => {
      isMounted = false;
    };
  }, [fromUtc, page, pageSize, status, traceId, toUtc]);

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / result.pageSize)) : 1;

  return (
    <section className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold text-white">Diagnostics · Errors</h1>
        <p className="text-sm text-slate-300">
          Review server-side error logs captured for 5xx responses.
        </p>
      </div>

      <div className="grid gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4 md:grid-cols-2">
        <Field label="Trace ID" hint="Filter by correlation/trace identifier.">
          <input
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
            placeholder="trace-id"
            value={traceId}
            onChange={(event) => {
              setTraceId(event.target.value);
              setPage(1);
            }}
          />
        </Field>
        <Field label="Status code" hint="Filter by HTTP status (e.g. 500).">
          <input
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
            placeholder="500"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value);
              setPage(1);
            }}
          />
        </Field>
        <Field label="From (local time)" hint="Optional range filter." error={filterError}>
          <input
            type="datetime-local"
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
            value={fromUtc}
            onChange={(event) => {
              setFromUtc(event.target.value);
              setPage(1);
            }}
            onBlur={() => setFilterError(validateAuditDateRange(fromUtc, toUtc))}
          />
        </Field>
        <Field label="To (local time)" hint="Optional range filter." error={filterError}>
          <input
            type="datetime-local"
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
            value={toUtc}
            onChange={(event) => {
              setToUtc(event.target.value);
              setPage(1);
            }}
            onBlur={() => setFilterError(validateAuditDateRange(fromUtc, toUtc))}
          />
        </Field>
        <div className="flex items-center gap-3">
          <label className="text-xs uppercase tracking-wide text-slate-400">Page size</label>
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
        <div className="md:col-span-2">
          <FormError message={formError} diagnostics={formDiagnostics} />
        </div>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-800">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-900 text-slate-300">
            <tr>
              <th className="px-4 py-3 font-medium">Timestamp</th>
              <th className="px-4 py-3 font-medium">Status</th>
              <th className="px-4 py-3 font-medium">Path</th>
              <th className="px-4 py-3 font-medium">Trace</th>
              <th className="px-4 py-3 font-medium">Actor</th>
              <th className="px-4 py-3 font-medium">Details</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800 bg-slate-950/40">
            {loading && (
              <tr>
                <td colSpan={6} className="px-4 py-6 text-center text-slate-400">
                  Loading error logs...
                </td>
              </tr>
            )}
            {!loading && result?.items.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-6 text-center text-slate-400">
                  No error logs found.
                </td>
              </tr>
            )}
            {result?.items.map((entry) => (
              <tr key={entry.id} className="text-slate-100">
                <td className="px-4 py-3 text-slate-300">
                  {new Date(entry.timestampUtc).toLocaleString()}
                </td>
                <td className="px-4 py-3 font-medium">{entry.statusCode}</td>
                <td className="px-4 py-3 text-slate-300">
                  <div className="font-medium text-slate-200">{entry.method}</div>
                  <div className="text-xs text-slate-400">{entry.path}</div>
                </td>
                <td className="px-4 py-3 text-slate-300">
                  <div className="text-xs">{entry.traceId}</div>
                </td>
                <td className="px-4 py-3 text-slate-300">
                  {entry.actorEmail ?? entry.actorUserId ?? "System"}
                </td>
                <td className="px-4 py-3 text-slate-300">
                  <Link
                    className="text-sm font-semibold text-indigo-300 hover:text-indigo-200"
                    to={toAdminRoute(`/diagnostics/errors/${entry.id}`)}
                  >
                    View details
                  </Link>
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
