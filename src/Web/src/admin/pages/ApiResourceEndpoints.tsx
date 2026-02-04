import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { ApiError, apiRequest } from "../api/http";
import type { AdminApiEndpointListItem, AdminPermissionItem } from "../api/types";
import { FormError, HelpIcon } from "../components/Field";
import { pushToast } from "../components/toast";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

type EditorState = {
  endpoint: AdminApiEndpointListItem;
  selected: Set<string>;
};

export default function ApiResourceEndpoints() {
  const { id } = useParams<{ id: string }>();
  const [endpoints, setEndpoints] = useState<AdminApiEndpointListItem[]>([]);
  const [permissions, setPermissions] = useState<AdminPermissionItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) {
      return;
    }
    let isMounted = true;
    const fetchData = async () => {
      setLoading(true);
      try {
        const [endpointData, permissionData] = await Promise.all([
          apiRequest<AdminApiEndpointListItem[]>(`/admin/api/api-resources/${id}/endpoints`),
          apiRequest<AdminPermissionItem[]>("/admin/api/permissions"),
        ]);
        if (isMounted) {
          setEndpoints(endpointData);
          setPermissions(permissionData);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };
    fetchData();
    return () => {
      isMounted = false;
    };
  }, [id]);

  const groupedPermissions = useMemo(() => {
    const grouped = new Map<string, AdminPermissionItem[]>();
    permissions.forEach((permission) => {
      const key = permission.group || "General";
      if (!grouped.has(key)) {
        grouped.set(key, []);
      }
      grouped.get(key)!.push(permission);
    });
    return Array.from(grouped.entries()).sort(([a], [b]) => a.localeCompare(b));
  }, [permissions]);

  const openEditor = (endpoint: AdminApiEndpointListItem) => {
    setEditor({
      endpoint,
      selected: new Set(endpoint.permissionIds),
    });
  };

  const togglePermission = (permissionId: string) => {
    if (!editor) {
      return;
    }
    setEditor((current) => {
      if (!current) {
        return current;
      }
      const next = new Set(current.selected);
      if (next.has(permissionId)) {
        next.delete(permissionId);
      } else {
        next.add(permissionId);
      }
      return { ...current, selected: next };
    });
  };

  const handleSave = async () => {
    if (!editor || !id) {
      return;
    }
    setSaving(true);
    setFormError(null);
    try {
      await apiRequest(
        `/admin/api/api-resources/${id}/endpoints/${editor.endpoint.id}/permissions`,
        {
          method: "PUT",
          body: JSON.stringify({ permissionIds: Array.from(editor.selected) }),
          suppressToast: true,
        }
      );
      pushToast({ message: "Endpoint permissions updated.", tone: "success" });
      setEndpoints((current) =>
        current.map((endpoint) =>
          endpoint.id === editor.endpoint.id
            ? {
                ...endpoint,
                permissionIds: Array.from(editor.selected),
                permissionKeys: permissions
                  .filter((permission) => editor.selected.has(permission.id))
                  .map((permission) => permission.key),
              }
            : endpoint
        )
      );
      setEditor(null);
    } catch (error) {
      if (error instanceof ApiError) {
        const parsed = parseProblemDetailsErrors(error);
        setFormError(parsed.generalError ?? "Unable to update endpoint permissions.");
        return;
      }
      setFormError("Unable to update endpoint permissions.");
    } finally {
      setSaving(false);
    }
  };

  if (!id) {
    return null;
  }

  return (
    <section className="flex flex-col gap-4">
      <div>
        <div className="flex items-center gap-2">
          <h1 className="text-2xl font-semibold text-white">API Endpoint Permissions</h1>
          <HelpIcon tooltip="Vyber permissiony pro konkrétní endpoint. Kombinace method + path musí být unikátní v rámci API resource. Endpoints se synchronizují z API služby." />
        </div>
        <p className="text-sm text-slate-300">Assign permissions to API endpoints.</p>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-800">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-900 text-slate-300">
            <tr>
              <th className="px-4 py-3 font-medium">Method</th>
              <th className="px-4 py-3 font-medium">Path</th>
              <th className="px-4 py-3 font-medium">Permissions</th>
              <th className="px-4 py-3 text-right font-medium">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800 bg-slate-950/40">
            {loading && (
              <tr>
                <td colSpan={4} className="px-4 py-6 text-center text-slate-400">
                  Loading endpoints...
                </td>
              </tr>
            )}
            {!loading && endpoints.length === 0 && (
              <tr>
                <td colSpan={4} className="px-4 py-6 text-center text-slate-400">
                  No endpoints registered. Sync endpoints from API service.
                </td>
              </tr>
            )}
            {endpoints.map((endpoint) => (
              <tr key={endpoint.id} className="text-slate-100">
                <td className="px-4 py-3 font-medium">{endpoint.method}</td>
                <td className="px-4 py-3 text-slate-300">{endpoint.path}</td>
                <td className="px-4 py-3 text-slate-300">
                  {endpoint.permissionKeys.length > 0 ? (
                    <div className="flex flex-wrap gap-2">
                      {endpoint.permissionKeys.map((key) => (
                        <span
                          key={key}
                          className="rounded-full bg-slate-800 px-2 py-1 text-xs text-slate-200"
                        >
                          {key}
                        </span>
                      ))}
                    </div>
                  ) : (
                    "-"
                  )}
                </td>
                <td className="px-4 py-3 text-right">
                  <button
                    type="button"
                    className="text-sm font-semibold text-indigo-300 hover:text-indigo-200"
                    onClick={() => openEditor(endpoint)}
                  >
                    Edit permissions
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {editor && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/70 px-4">
          <div className="w-full max-w-2xl rounded-lg border border-slate-800 bg-slate-950 p-6 shadow-lg">
            <div className="flex items-start justify-between gap-4">
              <div>
                <h2 className="text-lg font-semibold text-white">Edit permissions</h2>
                <p className="text-sm text-slate-300">
                  {editor.endpoint.method} {editor.endpoint.path}
                </p>
              </div>
              <button
                type="button"
                onClick={() => setEditor(null)}
                className="text-sm text-slate-400 hover:text-slate-200"
              >
                Close
              </button>
            </div>
            <div className="mt-4 max-h-[60vh] overflow-y-auto">
              <p className="mb-3 text-xs text-slate-400">
                Choose permissions required for this endpoint. You can leave it empty for public access.
              </p>
              <FormError message={formError} />
              {groupedPermissions.map(([group, items]) => (
                <div key={group} className="mb-4 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
                  <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-400">
                    {group}
                  </h3>
                  <div className="mt-3 grid gap-2 md:grid-cols-2">
                    {items.map((permission) => (
                      <label
                        key={permission.id}
                        className="flex items-start gap-3 rounded-md border border-slate-800 bg-slate-950/40 p-3"
                      >
                        <input
                          type="checkbox"
                          className="mt-1 h-4 w-4 rounded border-slate-600 bg-slate-900 text-indigo-400"
                          checked={editor.selected.has(permission.id)}
                          onChange={() => togglePermission(permission.id)}
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
            <div className="mt-6 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setEditor(null)}
                className="rounded-md border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-200 hover:bg-slate-900"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleSave}
                disabled={saving}
                className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60"
              >
                {saving ? "Saving..." : "Save permissions"}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
