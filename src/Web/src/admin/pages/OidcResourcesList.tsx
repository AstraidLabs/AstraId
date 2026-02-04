import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiRequest } from "../api/http";
import type {
  AdminOidcResourceListItem,
  AdminOidcResourceUsage,
  PagedResult,
} from "../api/types";
import ConfirmDialog from "../components/ConfirmDialog";
import { pushToast } from "../components/toast";
import { toAdminRoute } from "../../routing";

export default function OidcResourcesList() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [result, setResult] = useState<PagedResult<AdminOidcResourceListItem> | null>(null);
  const [loading, setLoading] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<AdminOidcResourceListItem | null>(null);
  const [deleteUsage, setDeleteUsage] = useState<AdminOidcResourceUsage | null>(null);
  const [usageLoading, setUsageLoading] = useState(false);

  useEffect(() => {
    let isMounted = true;
    const fetchResources = async () => {
      setLoading(true);
      const params = new URLSearchParams();
      if (search.trim()) {
        params.set("search", search.trim());
      }
      params.set("page", String(page));
      params.set("pageSize", String(pageSize));
      params.set("includeInactive", "true");
      try {
        const data = await apiRequest<PagedResult<AdminOidcResourceListItem>>(
          `/admin/api/oidc/resources?${params.toString()}`
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

    fetchResources();
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
        const usage = await apiRequest<AdminOidcResourceUsage>(
          `/admin/api/oidc/resources/${deleteTarget.id}/usage`
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
    await apiRequest(`/admin/api/oidc/resources/${deleteTarget.id}`, { method: "DELETE" });
    pushToast({ message: "Resource deactivated.", tone: "success" });
    setDeleteTarget(null);
    setResult((current) => {
      if (!current) {
        return current;
      }
      return {
        ...current,
        items: current.items.map((item) =>
          item.id === deleteTarget.id ? { ...item, isActive: false } : item
        ),
      };
    });
  };

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-white">OIDC Resources</h1>
          <p className="text-sm text-slate-300">
            Manage protected API resources used by OpenIddict scopes.
          </p>
        </div>
        <Link
          to={toAdminRoute("/oidc/resources/new")}
          className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400"
        >
          New resource
        </Link>
      </div>

      <div className="flex flex-wrap items-center gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <input
          className="w-full max-w-sm rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
          placeholder="Search by resource name or description..."
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
              <th className="px-4 py-3 font-medium">Status</th>
              <th className="px-4 py-3 font-medium">Updated</th>
              <th className="px-4 py-3 text-right font-medium">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800 bg-slate-950/40">
            {loading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-slate-400">
                  Loading resources...
                </td>
              </tr>
            )}
            {!loading && result?.items.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-slate-400">
                  No resources yet — create one.
                </td>
              </tr>
            )}
            {result?.items.map((resource) => (
              <tr key={resource.id} className="text-slate-100">
                <td className="px-4 py-3 font-medium">{resource.name}</td>
                <td className="px-4 py-3 text-slate-300">{resource.displayName ?? "-"}</td>
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
                <td className="px-4 py-3 text-slate-300">
                  {new Date(resource.updatedUtc).toLocaleDateString()}
                </td>
                <td className="px-4 py-3 text-right">
                  <div className="flex items-center justify-end gap-3">
                    <Link
                      to={toAdminRoute(`/oidc/resources/${resource.id}`)}
                      className="text-sm font-semibold text-indigo-300 hover:text-indigo-200"
                    >
                      Edit
                    </Link>
                    {resource.isActive && (
                      <button
                        type="button"
                        className="text-sm font-semibold text-rose-300 hover:text-rose-200"
                        onClick={() => setDeleteTarget(resource)}
                      >
                        Deactivate
                      </button>
                    )}
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
            Page {result.page} of {totalPages} · {result.totalCount} resources
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
        title="Deactivate resource?"
        description="This resource will be hidden from scope selection. Existing scopes will keep the reference."
        confirmLabel="Deactivate"
        confirmDisabled={(deleteUsage?.scopeCount ?? 0) > 0 || usageLoading}
        isOpen={Boolean(deleteTarget)}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={confirmDelete}
      >
        {deleteTarget && (
          <div className="space-y-2 text-sm text-slate-300">
            <p>
              Used by{" "}
              <span className="font-semibold text-slate-100">
                {usageLoading ? "..." : deleteUsage?.scopeCount ?? 0}
              </span>{" "}
              scopes.
            </p>
            {(deleteUsage?.scopeCount ?? 0) > 0 && (
              <p className="text-rose-300">
                Remove the resource from scopes before deactivating.
              </p>
            )}
          </div>
        )}
      </ConfirmDialog>
    </section>
  );
}
