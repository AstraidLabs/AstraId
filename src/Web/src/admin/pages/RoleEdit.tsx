import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { ApiError, apiRequest } from "../api/http";
import type { AdminPermissionItem, AdminRoleDetail } from "../api/types";
import { HelpIcon, FormError } from "../components/Field";
import { pushToast } from "../components/toast";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

type PermissionGroup = {
  group: string;
  permissions: AdminPermissionItem[];
};

export default function RoleEdit() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [role, setRole] = useState<AdminRoleDetail | null>(null);
  const [permissions, setPermissions] = useState<AdminPermissionItem[]>([]);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) {
      return;
    }
    let isMounted = true;
    const fetchRole = async () => {
      setLoading(true);
      try {
        const [roleData, permissionsData] = await Promise.all([
          apiRequest<AdminRoleDetail>(`/admin/api/roles/${id}`),
          apiRequest<AdminPermissionItem[]>("/admin/api/permissions"),
        ]);
        if (!isMounted) {
          return;
        }
        setRole(roleData);
        setPermissions(permissionsData);
        setSelected(new Set(roleData.permissionIds));
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };
    fetchRole();
    return () => {
      isMounted = false;
    };
  }, [id]);

  const groups = useMemo<PermissionGroup[]>(() => {
    const grouped = new Map<string, AdminPermissionItem[]>();
    permissions.forEach((permission) => {
      const key = permission.group || "General";
      if (!grouped.has(key)) {
        grouped.set(key, []);
      }
      grouped.get(key)!.push(permission);
    });
    return Array.from(grouped.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([group, items]) => ({
        group,
        permissions: items.sort((a, b) => a.key.localeCompare(b.key)),
      }));
  }, [permissions]);

  const handleToggle = (permissionId: string) => {
    setSelected((current) => {
      const next = new Set(current);
      if (next.has(permissionId)) {
        next.delete(permissionId);
      } else {
        next.add(permissionId);
      }
      return next;
    });
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!id) {
      return;
    }
    setFormError(null);
    setSaving(true);
    try {
      await apiRequest(`/admin/api/roles/${id}/permissions`, {
        method: "PUT",
        body: JSON.stringify({ permissionIds: Array.from(selected) }),
        suppressToast: true,
      });
      pushToast({ message: "Role permissions updated.", tone: "success" });
    } catch (error) {
      if (error instanceof ApiError) {
        const parsed = parseProblemDetailsErrors(error);
        setFormError(parsed.generalError ?? "Unable to update role permissions.");
        return;
      }
      setFormError("Unable to update role permissions.");
    } finally {
      setSaving(false);
    }
  };

  if (!id) {
    return null;
  }

  if (loading) {
    return (
      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6 text-slate-300">
        Loading role...
      </div>
    );
  }

  if (!role) {
    return (
      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6 text-slate-300">
        Role not found.
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <div>
        <div className="flex items-center gap-2">
          <h1 className="text-2xl font-semibold text-white">Role: {role.name}</h1>
          <HelpIcon tooltip="Role jsou Identity role. Oprávnění se mapují přes permissiony (claim permission). Změny ovlivní přístup uživatelů." />
        </div>
        <p className="text-sm text-slate-300">Assign permissions to control access.</p>
      </div>

      <FormError message={formError} />

      <div className="flex flex-col gap-6">
        {groups.map((group) => (
          <div key={group.group} className="rounded-lg border border-slate-800 bg-slate-900/40 p-6">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
              {group.group}
            </h2>
            <div className="mt-4 grid gap-3 md:grid-cols-2">
              {group.permissions.map((permission) => (
                <label
                  key={permission.id}
                  className="flex items-start gap-3 rounded-md border border-slate-800 bg-slate-950/40 p-3"
                >
                  <input
                    type="checkbox"
                    className="mt-1 h-4 w-4 rounded border-slate-600 bg-slate-900 text-indigo-400"
                    checked={selected.has(permission.id)}
                    onChange={() => handleToggle(permission.id)}
                  />
                  <div>
                    <div className="text-sm font-semibold text-slate-100">{permission.key}</div>
                    <div className="text-xs text-slate-400">{permission.description}</div>
                  </div>
                </label>
              ))}
            </div>
          </div>
        ))}
      </div>

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={saving}
          className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60"
        >
          {saving ? "Saving..." : "Save permissions"}
        </button>
        <button
          type="button"
          onClick={() => navigate(-1)}
          className="rounded-md border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-200 hover:bg-slate-900"
        >
          Cancel
        </button>
      </div>
    </form>
  );
}
