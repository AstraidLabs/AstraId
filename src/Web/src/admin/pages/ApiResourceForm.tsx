import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, apiRequest } from "../api/http";
import type { AdminApiResourceDetail } from "../api/types";
import { Field } from "../components/Field";
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
  baseUrl: string;
  isActive: boolean;
};

const defaultState: FormState = {
  name: "",
  displayName: "",
  baseUrl: "",
  isActive: true,
};

export default function ApiResourceForm({ mode, resourceId }: Props) {
  const navigate = useNavigate();
  const [form, setForm] = useState<FormState>(defaultState);
  const [loading, setLoading] = useState(mode === "edit");
  const [saving, setSaving] = useState(false);
  const [apiKey, setApiKey] = useState<string | null>(null);
  const [errors, setErrors] = useState<{ name?: string; displayName?: string; baseUrl?: string }>(
    {}
  );

  useEffect(() => {
    if (mode !== "edit" || !resourceId) {
      return;
    }
    let isMounted = true;
    const fetchResource = async () => {
      setLoading(true);
      try {
        const data = await apiRequest<AdminApiResourceDetail>(
          `/admin/api/api-resources/${resourceId}`
        );
        if (isMounted) {
          setForm({
            name: data.name,
            displayName: data.displayName,
            baseUrl: data.baseUrl ?? "",
            isActive: data.isActive,
          });
        }
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
    const nextErrors: { name?: string; displayName?: string; baseUrl?: string } = {};
    if (!form.name.trim()) {
      nextErrors.name = "Name is required.";
    }
    if (!form.displayName.trim()) {
      nextErrors.displayName = "Display name is required.";
    }
    if (form.baseUrl.trim()) {
      try {
        new URL(form.baseUrl.trim());
      } catch {
        nextErrors.baseUrl = "Base URL must be a valid absolute URL.";
      }
    }
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) {
      return;
    }
    setSaving(true);
    try {
      const payload = {
        name: form.name.trim(),
        displayName: form.displayName.trim(),
        baseUrl: form.baseUrl.trim() || null,
        isActive: form.isActive,
      };

      if (mode === "create") {
        const created = await apiRequest<AdminApiResourceDetail>("/admin/api/api-resources", {
          method: "POST",
          body: JSON.stringify(payload),
          suppressToast: true,
        });
        pushToast({ message: "API resource created.", tone: "success" });
        navigate(toAdminRoute(`/config/api-resources/${created.id}`));
        return;
      }

      if (!resourceId) {
        return;
      }

      await apiRequest<AdminApiResourceDetail>(`/admin/api/api-resources/${resourceId}`, {
        method: "PUT",
        body: JSON.stringify(payload),
        suppressToast: true,
      });
      pushToast({ message: "API resource updated.", tone: "success" });
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.fieldErrors) {
          setErrors((current) => ({
            ...current,
            name: error.fieldErrors.name?.[0] ?? current.name,
            displayName: error.fieldErrors.displayName?.[0] ?? current.displayName,
            baseUrl: error.fieldErrors.baseUrl?.[0] ?? current.baseUrl,
          }));
          return;
        }
        pushToast({ message: error.message, tone: "error" });
        return;
      }
      pushToast({ message: "Unable to save API resource.", tone: "error" });
    } finally {
      setSaving(false);
    }
  };

  const handleRotateKey = async () => {
    if (!resourceId) {
      return;
    }
    const data = await apiRequest<AdminApiResourceDetail>(
      `/admin/api/api-resources/${resourceId}/rotate-key`,
      { method: "POST" }
    );
    if (data.apiKey) {
      setApiKey(data.apiKey);
      pushToast({ message: "API key rotated.", tone: "success" });
    }
  };

  if (loading) {
    return (
      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6 text-slate-300">
        Loading API resource...
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-white">
            {mode === "create" ? "Create API resource" : "Edit API resource"}
          </h1>
          <p className="text-sm text-slate-300">Configure API metadata and status.</p>
        </div>
        {mode === "edit" && (
          <button
            type="button"
            onClick={handleRotateKey}
            className="rounded-md border border-amber-300/50 px-4 py-2 text-sm font-semibold text-amber-200 hover:bg-amber-400/10"
          >
            Rotate API key
          </button>
        )}
      </div>

      {apiKey && (
        <div className="rounded-lg border border-amber-500/40 bg-amber-500/10 p-4 text-sm text-amber-200">
          <p className="font-semibold">New API key</p>
          <code className="mt-2 block break-all rounded bg-amber-900/30 p-2 text-xs text-amber-100">
            {apiKey}
          </code>
          <p className="mt-2 text-xs text-amber-100">
            Store this value securely; it will not be shown again.
          </p>
        </div>
      )}

      <div className="grid gap-6 rounded-lg border border-slate-800 bg-slate-900/40 p-6 md:grid-cols-2">
        <Field
          label="Name"
          tooltip="Interní identifikátor API resource."
          hint="Povinné, např. identity-api."
          error={errors.name}
        >
          <input
            className={`rounded-md border bg-slate-950 px-3 py-2 text-slate-100 ${
              errors.name ? "border-rose-400" : "border-slate-700"
            }`}
            value={form.name}
            onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))}
            placeholder="identity-api"
          />
        </Field>

        <Field
          label="Display name"
          tooltip="Lidský název API pro administrátory."
          hint="Povinné."
          error={errors.displayName}
        >
          <input
            className={`rounded-md border bg-slate-950 px-3 py-2 text-slate-100 ${
              errors.displayName ? "border-rose-400" : "border-slate-700"
            }`}
            value={form.displayName}
            onChange={(event) => setForm((prev) => ({ ...prev, displayName: event.target.value }))}
            placeholder="Identity API"
          />
        </Field>

        <div className="md:col-span-2">
          <Field
            label="Base URL"
            tooltip="Základní URL API pro dokumentaci a odkazy."
            hint="Volitelné, musí být absolutní URL."
            error={errors.baseUrl}
          >
            <input
              className={`rounded-md border bg-slate-950 px-3 py-2 text-slate-100 ${
                errors.baseUrl ? "border-rose-400" : "border-slate-700"
              }`}
              value={form.baseUrl}
              onChange={(event) => setForm((prev) => ({ ...prev, baseUrl: event.target.value }))}
              placeholder="https://api.local.test"
            />
          </Field>
        </div>

        <div className="md:col-span-2">
          <Field
            label="Status"
            tooltip="Deaktivovaný API resource nelze používat pro sync endpointů."
            hint="Vypni, pokud je API mimo provoz."
          >
            <label className="flex items-center gap-3 text-sm text-slate-200">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(event) =>
                  setForm((prev) => ({ ...prev, isActive: event.target.checked }))
                }
                className="h-4 w-4 rounded border-slate-600 bg-slate-900 text-indigo-400"
              />
              Active
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
          {saving ? "Saving..." : "Save API resource"}
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
