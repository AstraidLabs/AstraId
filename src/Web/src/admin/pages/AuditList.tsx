import { useEffect, useState } from "react";
import { ApiError, apiRequest } from "../api/http";
import type { AdminAuditListItem, PagedResult } from "../api/types";
import { Field, FormError } from "../components/Field";
import { validateAuditDateRange } from "../validation/adminValidation";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

export default function AuditList() {
  const [search, setSearch] = useState("");
  const [action, setAction] = useState("");
  const [targetType, setTargetType] = useState("");
  const [targetId, setTargetId] = useState("");
  const [fromUtc, setFromUtc] = useState("");
  const [toUtc, setToUtc] = useState("");
  const [filterError, setFilterError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [result, setResult] = useState<PagedResult<AdminAuditListItem> | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const rangeError = validateAuditDateRange(fromUtc, toUtc);
    setFilterError(rangeError);
    if (rangeError) {
      return;
    }

    let isMounted = true;
    const fetchAudit = async () => {
      setLoading(true);
      const params = new URLSearchParams();
      if (search.trim()) {
        params.set("search", search.trim());
      }
      if (action.trim()) {
        params.set("action", action.trim());
      }
      if (targetType.trim()) {
        params.set("targetType", targetType.trim());
      }
      if (targetId.trim()) {
        params.set("targetId", targetId.trim());
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
        const data = await apiRequest<PagedResult<AdminAuditListItem>>(
          `/admin/api/audit?${params.toString()}`,
          { suppressToast: true }
        );
        if (isMounted) {
          setResult(data);
        }
        if (isMounted) {
          setFormError(null);
        }
      } catch (error) {
        if (error instanceof ApiError) {
          const parsed = parseProblemDetailsErrors(error);
          if (isMounted) {
            setFormError(parsed.generalError ?? "Unable to load audit entries.");
          }
        } else if (isMounted) {
          setFormError("Unable to load audit entries.");
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
  }, [action, fromUtc, page, pageSize, search, targetId, targetType, toUtc]);

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / result.pageSize)) : 1;

  return (
    <section className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold text-white">Audit log</h1>
        <p className="text-sm text-slate-300">Review admin actions captured by the server.</p>
      </div>

      <div className="grid gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4 md:grid-cols-2">
        <Field
          label="Search"
          tooltip="Hledá v action, target type a target id."
          hint="Použij volný text pro rychlé filtrování."
        >
          <input
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
            placeholder="Search action, target, or ID"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
          />
        </Field>
        <Field
          label="Action"
          tooltip="Filtrovat podle action klíče (např. client.updated)."
          hint="Použij přesný prefix."
        >
          <input
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
            placeholder="client.updated"
            value={action}
            onChange={(event) => {
              setAction(event.target.value);
              setPage(1);
            }}
          />
        </Field>
        <Field
          label="Target type"
          tooltip="Typ entity (např. User, Permission, ApiEndpoint)."
          hint="Volitelné."
        >
          <input
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
            placeholder="Permission"
            value={targetType}
            onChange={(event) => {
              setTargetType(event.target.value);
              setPage(1);
            }}
          />
        </Field>
        <Field
          label="Target ID"
          tooltip="ID cílového objektu."
          hint="Volitelné."
        >
          <input
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
            placeholder="GUID / ID"
            value={targetId}
            onChange={(event) => {
              setTargetId(event.target.value);
              setPage(1);
            }}
          />
        </Field>
        <Field
          label="From (local time)"
          tooltip="Audit log ukládá čas v UTC. Zadaný čas je lokální a převede se na UTC."
          hint="Volitelné."
          error={filterError}
        >
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
        <Field
          label="To (local time)"
          tooltip="Audit log ukládá čas v UTC. Zadaný čas je lokální a převede se na UTC."
          hint="Volitelné."
          error={filterError}
        >
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
          <FormError message={formError} />
        </div>
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
