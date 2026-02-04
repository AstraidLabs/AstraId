import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, apiRequest } from "../api/http";
import type { AdminPermissionItem } from "../api/types";
import { Field } from "../components/Field";
import { pushToast } from "../components/toast";
import { toAdminRoute } from "../../routing";

type Mode = "create" | "edit";

type Props = {
  mode: Mode;
  permissionId?: string;
};

type FormState = {
  key: string;
  description: string;
  group: string;
  isSystem: boolean;
};

const defaultState: FormState = {
  key: "",
  description: "",
  group: "",
  isSystem: false,
};

export default function PermissionForm({ mode, permissionId }: Props) {
  const navigate = useNavigate();
  const [form, setForm] = useState<FormState>(defaultState);
  const [errors, setErrors] = useState<{ key?: string; description?: string }>({});
  const [loading, setLoading] = useState(mode === "edit");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (mode !== "edit" || !permissionId) {
      return;
    }
    let isMounted = true;
    const fetchPermission = async () => {
      setLoading(true);
      try {
        const data = await apiRequest<AdminPermissionItem>(`/admin/api/permissions/${permissionId}`);
        if (isMounted) {
          setForm({
            key: data.key,
            description: data.description,
            group: data.group,
            isSystem: data.isSystem,
          });
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };
    fetchPermission();
    return () => {
      isMounted = false;
    };
  }, [mode, permissionId]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    const nextErrors: { key?: string; description?: string } = {};
    if (!form.key.trim()) {
      nextErrors.key = "Key is required.";
    }
    if (!form.description.trim()) {
      nextErrors.description = "Description is required.";
    }
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) {
      return;
    }

    setSaving(true);
    try {
      const payload = {
        key: form.key.trim(),
        description: form.description.trim(),
        group: form.group.trim(),
        isSystem: form.isSystem,
      };

      if (mode === "create") {
        await apiRequest<AdminPermissionItem>("/admin/api/permissions", {
          method: "POST",
          body: JSON.stringify(payload),
          suppressToast: true,
        });
        pushToast({ message: "Permission created.", tone: "success" });
        navigate(toAdminRoute("/config/permissions"));
        return;
      }

      if (!permissionId) {
        return;
      }
      await apiRequest<AdminPermissionItem>(`/admin/api/permissions/${permissionId}`, {
        method: "PUT",
        body: JSON.stringify(payload),
        suppressToast: true,
      });
      pushToast({ message: "Permission updated.", tone: "success" });
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.fieldErrors) {
          setErrors((current) => ({
            ...current,
            key: error.fieldErrors.key?.[0] ?? current.key,
            description: error.fieldErrors.description?.[0] ?? current.description,
          }));
          return;
        }
        pushToast({ message: error.message, tone: "error" });
        return;
      }
      pushToast({ message: "Unable to save permission.", tone: "error" });
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6 text-slate-300">
        Loading permission...
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">
          {mode === "create" ? "Create permission" : "Edit permission"}
        </h1>
        <p className="text-sm text-slate-300">Configure application permission metadata.</p>
      </div>

      <div className="grid gap-6 rounded-lg border border-slate-800 bg-slate-900/40 p-6 md:grid-cols-2">
        <Field
          label="Key"
          tooltip="Jednoznačný identifikátor permission. Používej tečky pro hierarchii."
          hint="Příklad: system.admin, oidc.clients.read."
          error={errors.key}
        >
          <input
            className={`rounded-md border bg-slate-950 px-3 py-2 text-slate-100 ${
              errors.key ? "border-rose-400" : "border-slate-700"
            }`}
            value={form.key}
            onChange={(event) => setForm((prev) => ({ ...prev, key: event.target.value }))}
            onBlur={() =>
              setErrors((current) => ({
                ...current,
                key: form.key.trim() ? undefined : "Key is required.",
              }))
            }
            placeholder="system.admin"
          />
        </Field>

        <Field
          label="Group"
          tooltip="Skupina pro vizuální seskupení permission."
          hint="Volitelné, např. System nebo OIDC."
        >
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            value={form.group}
            onChange={(event) => setForm((prev) => ({ ...prev, group: event.target.value }))}
            placeholder="System"
          />
        </Field>

        <div className="md:col-span-2">
          <Field
            label="Description"
            tooltip="Krátký popis pro administrátory."
            hint="Povinné."
            error={errors.description}
          >
            <textarea
              className={`min-h-[120px] rounded-md border bg-slate-950 px-3 py-2 text-slate-100 ${
                errors.description ? "border-rose-400" : "border-slate-700"
              }`}
              value={form.description}
              onChange={(event) =>
                setForm((prev) => ({ ...prev, description: event.target.value }))
              }
              onBlur={() =>
                setErrors((current) => ({
                  ...current,
                  description: form.description.trim() ? undefined : "Description is required.",
                }))
              }
              placeholder="Allows managing OIDC clients."
            />
          </Field>
        </div>

        <div className="md:col-span-2">
          <Field
            label="System permission"
            tooltip="System permissiony jsou chráněné proti smazání."
            hint="Zapni pouze pro core oprávnění."
          >
            <label className="flex items-center gap-3 text-sm text-slate-200">
              <input
                type="checkbox"
                checked={form.isSystem}
                onChange={(event) =>
                  setForm((prev) => ({ ...prev, isSystem: event.target.checked }))
                }
                className="h-4 w-4 rounded border-slate-600 bg-slate-900 text-indigo-400"
              />
              Protected from deletion
            </label>
          </Field>
        </div>
      </div>

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={saving}
          className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60"
        >
          {saving ? "Saving..." : "Save permission"}
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
