import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ApiError, apiRequest } from "../api/http";
import type { AdminRoleListItem, AdminRoleUsage } from "../api/types";
import ConfirmDialog from "../components/ConfirmDialog";
import { Field, FormError } from "../components/Field";
import { pushToast } from "../components/toast";
import { toAdminRoute } from "../../routing";
import { validateRoleName } from "../validation/adminValidation";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

export default function RolesList() {
  const [roles, setRoles] = useState<AdminRoleListItem[]>([]);
  const [newRoleName, setNewRoleName] = useState("");
  const [newRoleError, setNewRoleError] = useState<string | null>(null);
  const [newRoleFormError, setNewRoleFormError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [deleteTarget, setDeleteTarget] = useState<AdminRoleListItem | null>(null);
  const [deleteUsage, setDeleteUsage] = useState<AdminRoleUsage | null>(null);
  const [usageLoading, setUsageLoading] = useState(false);
  const [editingRoleId, setEditingRoleId] = useState<string | null>(null);
  const [editingName, setEditingName] = useState("");
  const [editingError, setEditingError] = useState<string | null>(null);
  const [editingFormError, setEditingFormError] = useState<string | null>(null);
  const [savingRoleId, setSavingRoleId] = useState<string | null>(null);

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

  useEffect(() => {
    if (!deleteTarget) {
      setDeleteUsage(null);
      return;
    }
    let isMounted = true;
    const fetchUsage = async () => {
      setUsageLoading(true);
      try {
        const usage = await apiRequest<AdminRoleUsage>(`/admin/api/roles/${deleteTarget.id}/usage`);
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

  const handleCreate = async (event: React.FormEvent) => {
    event.preventDefault();
    const validation = validateRoleName(newRoleName);
    if (validation.error) {
      setNewRoleError(validation.error);
      return;
    }
    setNewRoleError(null);
    setNewRoleFormError(null);
    try {
      await apiRequest("/admin/api/roles", {
        method: "POST",
        body: JSON.stringify({ name: validation.value }),
        suppressToast: true,
      });
      pushToast({ message: "Role created.", tone: "success" });
      setNewRoleName("");
      await fetchRoles();
    } catch (error) {
      if (error instanceof ApiError) {
        const parsed = parseProblemDetailsErrors(error);
        setNewRoleError(parsed.fieldErrors.name?.[0] ?? null);
        setNewRoleFormError(parsed.generalError ?? "Unable to create role.");
        return;
      }
      setNewRoleFormError("Unable to create role.");
    }
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

  const startEditing = (role: AdminRoleListItem) => {
    setEditingRoleId(role.id);
    setEditingName(role.name);
    setEditingError(null);
  };

  const cancelEditing = () => {
    setEditingRoleId(null);
    setEditingName("");
  };

  const saveEditing = async () => {
    if (!editingRoleId) {
      return;
    }
    const validation = validateRoleName(editingName);
    if (validation.error) {
      setEditingError(validation.error);
      return;
    }
    setEditingError(null);
    setEditingFormError(null);
    setSavingRoleId(editingRoleId);
    try {
      await apiRequest(`/admin/api/roles/${editingRoleId}`, {
        method: "PUT",
        body: JSON.stringify({ name: validation.value }),
        suppressToast: true,
      });
      pushToast({ message: "Role updated.", tone: "success" });
      setRoles((current) =>
        current.map((role) =>
          role.id === editingRoleId ? { ...role, name: validation.value } : role
        )
      );
      cancelEditing();
    } catch (error) {
      if (error instanceof ApiError) {
        const parsed = parseProblemDetailsErrors(error);
        setEditingError(parsed.fieldErrors.name?.[0] ?? null);
        setEditingFormError(parsed.generalError ?? "Unable to update role.");
        return;
      }
      setEditingFormError("Unable to update role.");
    } finally {
      setSavingRoleId(null);
    }
  };

  return (
    <section className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold text-white">Roles</h1>
        <p className="text-sm text-slate-300">Assign permissions to roles for admin access control.</p>
      </div>

      <form
        onSubmit={handleCreate}
        className="flex flex-wrap items-end gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4"
      >
        <div className="w-full max-w-sm">
          <Field
            label="New role"
            tooltip="Role je Identity role. Oprávnění se mapují přes permissiony."
            hint="Používej krátké názvy jako Admin, Support, Editor."
            error={newRoleError}
            required
          >
            <input
              className={`w-full rounded-md border bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500 ${
                newRoleError ? "border-rose-400" : "border-slate-700"
              }`}
              placeholder="Admin"
              value={newRoleName}
              onChange={(event) => setNewRoleName(event.target.value)}
              onBlur={() =>
                setNewRoleError(validateRoleName(newRoleName).error ?? null)
              }
            />
          </Field>
        </div>
        <button
          type="submit"
          className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400"
        >
          Create role
        </button>
        <div className="w-full">
          <FormError message={newRoleFormError} />
        </div>
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
                <td className="px-4 py-3 font-medium">
                  {editingRoleId === role.id ? (
                    <input
                      className={`w-full rounded-md border bg-slate-950 px-3 py-2 text-sm text-slate-100 ${
                        editingError ? "border-rose-400" : "border-slate-700"
                      }`}
                      value={editingName}
                      onChange={(event) => setEditingName(event.target.value)}
                      onBlur={() =>
                        setEditingError(validateRoleName(editingName).error ?? null)
                      }
                    />
                  ) : (
                    role.name
                  )}
                  {editingRoleId === role.id && editingError && (
                    <div className="mt-1 text-xs text-rose-300">{editingError}</div>
                  )}
                  {editingRoleId === role.id && <FormError message={editingFormError} />}
                </td>
                <td className="px-4 py-3 text-slate-300">{role.isSystem ? "Yes" : "No"}</td>
                <td className="px-4 py-3 text-right">
                  <div className="flex items-center justify-end gap-3">
                    <Link
                      to={toAdminRoute(`/config/roles/${role.id}`)}
                      className="text-sm font-semibold text-indigo-300 hover:text-indigo-200"
                    >
                      Edit permissions
                    </Link>
                    {!role.isSystem && editingRoleId !== role.id && (
                      <button
                        type="button"
                        className="text-sm font-semibold text-slate-300 hover:text-slate-200"
                        onClick={() => startEditing(role)}
                      >
                        Rename
                      </button>
                    )}
                    {!role.isSystem && editingRoleId === role.id && (
                      <>
                        <button
                          type="button"
                          className="text-sm font-semibold text-emerald-300 hover:text-emerald-200"
                          onClick={saveEditing}
                          disabled={savingRoleId === role.id}
                        >
                          {savingRoleId === role.id ? "Saving..." : "Save"}
                        </button>
                        <button
                          type="button"
                          className="text-sm font-semibold text-slate-300 hover:text-slate-200"
                          onClick={cancelEditing}
                          disabled={savingRoleId === role.id}
                        >
                          Cancel
                        </button>
                      </>
                    )}
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
        confirmDisabled={(deleteUsage?.userCount ?? 0) > 0 || usageLoading}
        isOpen={Boolean(deleteTarget)}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={confirmDelete}
      >
        {deleteTarget && (
          <div className="space-y-2 text-sm text-slate-300">
            <p>
              Assigned to{" "}
              <span className="font-semibold text-slate-100">
                {usageLoading ? "..." : deleteUsage?.userCount ?? 0}
              </span>{" "}
              users.
            </p>
            {(deleteUsage?.userCount ?? 0) > 0 && (
              <p className="text-rose-300">
                Remove users from the role before deleting.
              </p>
            )}
          </div>
        )}
      </ConfirmDialog>
    </section>
  );
}
