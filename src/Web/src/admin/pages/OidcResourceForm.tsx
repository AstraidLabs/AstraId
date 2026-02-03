import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiRequest } from "../api/http";
import type { AdminOidcResourceDetail } from "../api/types";
import { pushToast } from "../components/toast";
import { toAdminRoute } from "../../routing";

type Mode = "create" | "edit";

type Props = {
  mode: Mode;
  resourceId?: string;
};

type FormState = {
  name: string;
  displayName: string;
  description: string;
  isActive: boolean;
};

const defaultState: FormState = {
  name: "",
  displayName: "",
  description: "",
  isActive: true,
};

export default function OidcResourceForm({ mode, resourceId }: Props) {
  const navigate = useNavigate();
  const [form, setForm] = useState<FormState>(defaultState);
  const [errors, setErrors] = useState<{ name?: string }>({});
  const [loading, setLoading] = useState(mode === "edit");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (mode !== "edit" || !resourceId) {
      return;
    }

    let isMounted = true;
    const fetchResource = async () => {
      setLoading(true);
      try {
        const data = await apiRequest<AdminOidcResourceDetail>(
          `/admin/api/oidc/resources/${resourceId}`
        );
        if (!isMounted) {
          return;
        }
        setForm({
          name: data.name,
          displayName: data.displayName ?? "",
          description: data.description ?? "",
          isActive: data.isActive,
        });
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };
    fetchResource();
    return () => {
      isMounted = false;
    };
  }, [mode, resourceId]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    const trimmedName = form.name.trim();
    if (!trimmedName) {
      setErrors({ name: "Resource name is required." });
      return;
    }
    setErrors({});
    setSaving(true);
    try {
      const payload = {
        name: trimmedName,
        displayName: form.displayName.trim() || null,
        description: form.description.trim() || null,
        isActive: form.isActive,
      };

      if (mode === "create") {
        const created = await apiRequest<AdminOidcResourceDetail>("/admin/api/oidc/resources", {
          method: "POST",
          body: JSON.stringify(payload),
        });
        pushToast({ message: "Resource created.", tone: "success" });
        navigate(toAdminRoute(`/oidc/resources/${created.id}`));
        return;
      }

      if (!resourceId) {
        return;
      }

      await apiRequest<AdminOidcResourceDetail>(`/admin/api/oidc/resources/${resourceId}`, {
        method: "PUT",
        body: JSON.stringify(payload),
      });
      pushToast({ message: "Resource updated.", tone: "success" });
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6 text-slate-300">
        Loading resource...
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">
          {mode === "create" ? "Create resource" : "Edit resource"}
        </h1>
        <p className="text-sm text-slate-300">Resources are used as scope targets in OpenIddict.</p>
      </div>

      <div className="grid gap-6 rounded-lg border border-slate-800 bg-slate-900/40 p-6 md:grid-cols-2">
        <label className="flex flex-col gap-2 text-sm">
          <span className="text-slate-200">Name</span>
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            value={form.name}
            onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))}
            placeholder="api.read"
          />
          {errors.name && <span className="text-xs text-rose-300">{errors.name}</span>}
        </label>

        <label className="flex flex-col gap-2 text-sm">
          <span className="text-slate-200">Display name</span>
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            value={form.displayName}
            onChange={(event) => setForm((prev) => ({ ...prev, displayName: event.target.value }))}
            placeholder="API Read"
          />
        </label>

        <label className="flex flex-col gap-2 text-sm md:col-span-2">
          <span className="text-slate-200">Description</span>
          <textarea
            className="min-h-[120px] rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            value={form.description}
            onChange={(event) => setForm((prev) => ({ ...prev, description: event.target.value }))}
            placeholder="Optional description for admins."
          />
        </label>

        <label className="flex items-center gap-3 text-sm text-slate-200 md:col-span-2">
          <input
            type="checkbox"
            checked={form.isActive}
            onChange={(event) => setForm((prev) => ({ ...prev, isActive: event.target.checked }))}
            className="h-4 w-4 rounded border-slate-600 bg-slate-900 text-indigo-400"
          />
          Active (available for scope selection)
        </label>
      </div>

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={saving}
          className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60"
        >
          {saving ? "Saving..." : "Save resource"}
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
