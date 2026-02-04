import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiRequest } from "../api/http";
import type { AdminOidcScopeListItem, AdminOidcScopeUsage, PagedResult } from "../api/types";
import ConfirmDialog from "../components/ConfirmDialog";
import { pushToast } from "../components/toast";
import { toAdminRoute } from "../../routing";

export default function OidcScopesList() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [result, setResult] = useState<PagedResult<AdminOidcScopeListItem> | null>(null);
  const [loading, setLoading] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<AdminOidcScopeListItem | null>(null);
  const [deleteUsage, setDeleteUsage] = useState<AdminOidcScopeUsage | null>(null);
  const [usageLoading, setUsageLoading] = useState(false);

  useEffect(() => {
    let isMounted = true;
    const fetchScopes = async () => {
      setLoading(true);
      const params = new URLSearchParams();
      if (search.trim()) {
        params.set("search", search.trim());
      }
      params.set("page", String(page));
      params.set("pageSize", String(pageSize));
      try {
        const data = await apiRequest<PagedResult<AdminOidcScopeListItem>>(
          `/admin/api/oidc/scopes?${params.toString()}`
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

    fetchScopes();
    return () => {
      isMounted = false;
    };
  }, [page, pageSize, search]);

  useEffect(() => {
    if (!deleteTarget) {
      setDeleteUsage(null);
      return;
    }
    let isMounted = true;
    const fetchUsage = async () => {
      setUsageLoading(true);
      try {
        const usage = await apiRequest<AdminOidcScopeUsage>(
          `/admin/api/oidc/scopes/${deleteTarget.id}/usage`
        );
        if (isMounted) {
          setDeleteUsage(usage);
        }
      } finally {
        if (isMounted) {
          setUsageLoading(false);
        }
      }
    };
    fetchUsage();
    return () => {
      isMounted = false;
    };
  }, [deleteTarget]);

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / result.pageSize)) : 1;

  const confirmDelete = async () => {
    if (!deleteTarget) {
      return;
    }
    await apiRequest(`/admin/api/oidc/scopes/${deleteTarget.id}`, { method: "DELETE" });
    pushToast({ message: "Scope deleted.", tone: "success" });
    setDeleteTarget(null);
    setResult((current) => {
      if (!current) {
        return current;
      }
      return {
        ...current,
        items: current.items.filter((item) => item.id !== deleteTarget.id),
        totalCount: Math.max(0, current.totalCount - 1),
      };
    });
  };

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-white">OIDC Scopes</h1>
          <p className="text-sm text-slate-300">
            Manage OpenIddict scopes and their bound resources.
          </p>
        </div>
        <Link
          to={toAdminRoute("/oidc/scopes/new")}
          className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400"
        >
          New scope
        </Link>
      </div>

      <div className="flex flex-wrap items-center gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <input
          className="w-full max-w-sm rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
          placeholder="Search by scope name or display name..."
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
              <th className="px-4 py-3 font-medium">Name</th>
              <th className="px-4 py-3 font-medium">Display name</th>
              <th className="px-4 py-3 font-medium">Resources</th>
              <th className="px-4 py-3 text-right font-medium">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800 bg-slate-950/40">
            {loading && (
              <tr>
                <td colSpan={4} className="px-4 py-6 text-center text-slate-400">
                  Loading scopes...
                </td>
              </tr>
            )}
            {!loading && result?.items.length === 0 && (
              <tr>
                <td colSpan={4} className="px-4 py-6 text-center text-slate-400">
                  No scopes yet — create one.
                </td>
              </tr>
            )}
            {result?.items.map((scope) => (
              <tr key={scope.id} className="text-slate-100">
                <td className="px-4 py-3 font-medium">{scope.name}</td>
                <td className="px-4 py-3 text-slate-300">{scope.displayName ?? "-"}</td>
                <td className="px-4 py-3 text-slate-300">
                  {scope.resources.length > 0 ? (
                    <div className="flex flex-wrap gap-2">
                      {scope.resources.map((resource) => (
                        <span
                          key={resource}
                          className="rounded-full bg-slate-800 px-2 py-1 text-xs text-slate-200"
                        >
                          {resource}
                        </span>
                      ))}
                    </div>
                  ) : (
                    "-"
                  )}
                </td>
                <td className="px-4 py-3 text-right">
                  <div className="flex items-center justify-end gap-3">
                    <Link
                      to={toAdminRoute(`/oidc/scopes/${scope.id}`)}
                      className="text-sm font-semibold text-indigo-300 hover:text-indigo-200"
                    >
                      Edit
                    </Link>
                    <button
                      type="button"
                      className="text-sm font-semibold text-rose-300 hover:text-rose-200"
                      onClick={() => setDeleteTarget(scope)}
                    >
                      Delete
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {result && (
        <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-slate-300">
          <span>
            Page {result.page} of {totalPages} · {result.totalCount} scopes
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

      <ConfirmDialog
        title="Delete scope?"
        description="This will permanently remove the scope from OpenIddict."
        confirmLabel="Delete"
        confirmDisabled={(deleteUsage?.clientCount ?? 0) > 0 || usageLoading}
        isOpen={Boolean(deleteTarget)}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={confirmDelete}
      >
        {deleteTarget && (
          <div className="space-y-2 text-sm text-slate-300">
            <p>
              Used by{" "}
              <span className="font-semibold text-slate-100">
                {usageLoading ? "..." : deleteUsage?.clientCount ?? 0}
              </span>{" "}
              clients.
            </p>
            {(deleteUsage?.clientCount ?? 0) > 0 && (
              <p className="text-rose-300">
                Remove the scope from clients before deleting.
              </p>
            )}
          </div>
        )}
      </ConfirmDialog>
    </section>
  );
}
