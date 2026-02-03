import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiRequest } from "../api/http";
import type { AdminRoleListItem } from "../api/types";
import ConfirmDialog from "../components/ConfirmDialog";
import { pushToast } from "../components/toast";

export default function RolesList() {
  const [roles, setRoles] = useState<AdminRoleListItem[]>([]);
  const [newRoleName, setNewRoleName] = useState("");
  const [loading, setLoading] = useState(true);
  const [deleteTarget, setDeleteTarget] = useState<AdminRoleListItem | null>(null);

  const fetchRoles = async () => {
    const data = await apiRequest<AdminRoleListItem[]>("/admin/api/roles");
    setRoles(data);
  };

  useEffect(() => {
    let isMounted = true;
    const load = async () => {
      try {
        await fetchRoles();
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };
    load();
    return () => {
      isMounted = false;
    };
  }, []);

  const handleCreate = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!newRoleName.trim()) {
      return;
    }
    await apiRequest("/admin/api/roles", {
      method: "POST",
      body: JSON.stringify({ name: newRoleName.trim() }),
    });
    pushToast({ message: "Role created.", tone: "success" });
    setNewRoleName("");
    await fetchRoles();
  };

  const confirmDelete = async () => {
    if (!deleteTarget) {
      return;
    }
    await apiRequest(`/admin/api/roles/${deleteTarget.id}`, { method: "DELETE" });
    pushToast({ message: "Role deleted.", tone: "success" });
    setRoles((current) => current.filter((role) => role.id !== deleteTarget.id));
    setDeleteTarget(null);
  };

  return (
    <section className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold text-white">Roles</h1>
        <p className="text-sm text-slate-300">Assign permissions to roles for admin access control.</p>
      </div>

      <form
        onSubmit={handleCreate}
        className="flex flex-wrap items-center gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4"
      >
        <input
          className="w-full max-w-sm rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
          placeholder="New role name..."
          value={newRoleName}
          onChange={(event) => setNewRoleName(event.target.value)}
        />
        <button
          type="submit"
          className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400"
        >
          Create role
        </button>
      </form>

      <div className="overflow-hidden rounded-lg border border-slate-800">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-900 text-slate-300">
            <tr>
              <th className="px-4 py-3 font-medium">Role</th>
              <th className="px-4 py-3 font-medium">System</th>
              <th className="px-4 py-3 text-right font-medium">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800 bg-slate-950/40">
            {loading && (
              <tr>
                <td colSpan={3} className="px-4 py-6 text-center text-slate-400">
                  Loading roles...
                </td>
              </tr>
            )}
            {!loading && roles.length === 0 && (
              <tr>
                <td colSpan={3} className="px-4 py-6 text-center text-slate-400">
                  No roles found.
                </td>
              </tr>
            )}
            {roles.map((role) => (
              <tr key={role.id} className="text-slate-100">
                <td className="px-4 py-3 font-medium">{role.name}</td>
                <td className="px-4 py-3 text-slate-300">{role.isSystem ? "Yes" : "No"}</td>
                <td className="px-4 py-3 text-right">
                  <div className="flex items-center justify-end gap-3">
                    <Link
                      to={`/config/roles/${role.id}`}
                      className="text-sm font-semibold text-indigo-300 hover:text-indigo-200"
                    >
                      Edit permissions
                    </Link>
                    {!role.isSystem && (
                      <button
                        type="button"
                        className="text-sm font-semibold text-rose-300 hover:text-rose-200"
                        onClick={() => setDeleteTarget(role)}
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
        title="Delete role?"
        description="This will remove the role and its permission assignments."
        confirmLabel="Delete"
        isOpen={Boolean(deleteTarget)}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={confirmDelete}
      />
    </section>
  );
}
