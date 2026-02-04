import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiRequest } from "../api/http";
import type { AdminPermissionItem, AdminPermissionUsage } from "../api/types";
import ConfirmDialog from "../components/ConfirmDialog";
import { pushToast } from "../components/toast";
import { toAdminRoute } from "../../routing";

export default function PermissionsList() {
  const [permissions, setPermissions] = useState<AdminPermissionItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [deleteTarget, setDeleteTarget] = useState<AdminPermissionItem | null>(null);
  const [deleteUsage, setDeleteUsage] = useState<AdminPermissionUsage | null>(null);
  const [usageLoading, setUsageLoading] = useState(false);

  useEffect(() => {
    let isMounted = true;
    const fetchPermissions = async () => {
      try {
        const data = await apiRequest<AdminPermissionItem[]>("/admin/api/permissions");
        if (isMounted) {
          setPermissions(data);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };
    fetchPermissions();
    return () => {
      isMounted = false;
    };
  }, []);

  useEffect(() => {
    if (!deleteTarget) {
      setDeleteUsage(null);
      return;
    }
    let isMounted = true;
    const fetchUsage = async () => {
      setUsageLoading(true);
      try {
        const usage = await apiRequest<AdminPermissionUsage>(
          `/admin/api/permissions/${deleteTarget.id}/usage`
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

  const confirmDelete = async () => {
    if (!deleteTarget) {
      return;
    }
    await apiRequest(`/admin/api/permissions/${deleteTarget.id}`, { method: "DELETE" });
    pushToast({ message: "Permission deleted.", tone: "success" });
    setPermissions((current) => current.filter((item) => item.id !== deleteTarget.id));
    setDeleteTarget(null);
  };

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-white">Permissions</h1>
          <p className="text-sm text-slate-300">Manage permission definitions and grouping.</p>
        </div>
        <Link
          to={toAdminRoute("/config/permissions/new")}
          className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400"
        >
          New permission
        </Link>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-800">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-900 text-slate-300">
            <tr>
              <th className="px-4 py-3 font-medium">Key</th>
              <th className="px-4 py-3 font-medium">Description</th>
              <th className="px-4 py-3 font-medium">Group</th>
              <th className="px-4 py-3 font-medium">System</th>
              <th className="px-4 py-3 text-right font-medium">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800 bg-slate-950/40">
            {loading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-slate-400">
                  Loading permissions...
                </td>
              </tr>
            )}
            {!loading && permissions.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-slate-400">
                  No permissions yet — create one.
                </td>
              </tr>
            )}
            {permissions.map((permission) => (
              <tr key={permission.id} className="text-slate-100">
                <td className="px-4 py-3 font-medium">{permission.key}</td>
                <td className="px-4 py-3 text-slate-300">{permission.description}</td>
                <td className="px-4 py-3 text-slate-300">{permission.group || "-"}</td>
                <td className="px-4 py-3 text-slate-300">
                  {permission.isSystem ? "Yes" : "No"}
                </td>
                <td className="px-4 py-3 text-right">
                  <div className="flex items-center justify-end gap-3">
                    <Link
                      to={toAdminRoute(`/config/permissions/${permission.id}`)}
                      className="text-sm font-semibold text-indigo-300 hover:text-indigo-200"
                    >
                      Edit
                    </Link>
                    {!permission.isSystem && (
                      <button
                        type="button"
                        className="text-sm font-semibold text-rose-300 hover:text-rose-200"
                        onClick={() => setDeleteTarget(permission)}
                      >
                        Delete
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ConfirmDialog
        title="Delete permission?"
        description="This will remove the permission and related mappings."
        confirmLabel="Delete"
        confirmDisabled={(deleteUsage?.roleCount ?? 0) > 0 || (deleteUsage?.endpointCount ?? 0) > 0 || usageLoading}
        isOpen={Boolean(deleteTarget)}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={confirmDelete}
      >
        {deleteTarget && (
          <div className="space-y-2 text-sm text-slate-300">
            <p>
              Used by{" "}
              <span className="font-semibold text-slate-100">
                {usageLoading ? "..." : deleteUsage?.roleCount ?? 0}
              </span>{" "}
              roles and{" "}
              <span className="font-semibold text-slate-100">
                {usageLoading ? "..." : deleteUsage?.endpointCount ?? 0}
              </span>{" "}
              endpoints.
            </p>
            {((deleteUsage?.roleCount ?? 0) > 0 || (deleteUsage?.endpointCount ?? 0) > 0) && (
              <p className="text-rose-300">Remove assignments before deleting.</p>
            )}
          </div>
        )}
      </ConfirmDialog>
    </section>
  );
}
