import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiRequest } from "../api/http";
import type { AdminPermissionItem } from "../api/types";
import { pushToast } from "../components/toast";

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
        });
        pushToast({ message: "Permission created.", tone: "success" });
        navigate("/config/permissions");
        return;
      }

      if (!permissionId) {
        return;
      }
      await apiRequest<AdminPermissionItem>(`/admin/api/permissions/${permissionId}`, {
        method: "PUT",
        body: JSON.stringify(payload),
      });
      pushToast({ message: "Permission updated.", tone: "success" });
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
        <label className="flex flex-col gap-2 text-sm">
          <span className="text-slate-200">Key</span>
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            value={form.key}
            onChange={(event) => setForm((prev) => ({ ...prev, key: event.target.value }))}
            placeholder="system.admin"
          />
          {errors.key && <span className="text-xs text-rose-300">{errors.key}</span>}
        </label>

        <label className="flex flex-col gap-2 text-sm">
          <span className="text-slate-200">Group</span>
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            value={form.group}
            onChange={(event) => setForm((prev) => ({ ...prev, group: event.target.value }))}
            placeholder="System"
          />
        </label>

        <label className="flex flex-col gap-2 text-sm md:col-span-2">
          <span className="text-slate-200">Description</span>
          <textarea
            className="min-h-[120px] rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            value={form.description}
            onChange={(event) => setForm((prev) => ({ ...prev, description: event.target.value }))}
          />
          {errors.description && <span className="text-xs text-rose-300">{errors.description}</span>}
        </label>

        <label className="flex items-center gap-3 text-sm text-slate-200 md:col-span-2">
          <input
            type="checkbox"
            checked={form.isSystem}
            onChange={(event) => setForm((prev) => ({ ...prev, isSystem: event.target.checked }))}
            className="h-4 w-4 rounded border-slate-600 bg-slate-900 text-indigo-400"
          />
          System permission (protected from deletion)
        </label>
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
