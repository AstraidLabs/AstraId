import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiRequest } from "../api/http";
import type { AdminApiResourceDetail } from "../api/types";
import { pushToast } from "../components/toast";

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
    if (!form.name.trim() || !form.displayName.trim()) {
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
        });
        pushToast({ message: "API resource created.", tone: "success" });
        navigate(`/config/api-resources/${created.id}`);
        return;
      }

      if (!resourceId) {
        return;
      }

      await apiRequest<AdminApiResourceDetail>(`/admin/api/api-resources/${resourceId}`, {
        method: "PUT",
        body: JSON.stringify(payload),
      });
      pushToast({ message: "API resource updated.", tone: "success" });
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
        <label className="flex flex-col gap-2 text-sm">
          <span className="text-slate-200">Name</span>
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            value={form.name}
            onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))}
            placeholder="identity-api"
          />
        </label>

        <label className="flex flex-col gap-2 text-sm">
          <span className="text-slate-200">Display name</span>
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            value={form.displayName}
            onChange={(event) => setForm((prev) => ({ ...prev, displayName: event.target.value }))}
            placeholder="Identity API"
          />
        </label>

        <label className="flex flex-col gap-2 text-sm md:col-span-2">
          <span className="text-slate-200">Base URL</span>
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            value={form.baseUrl}
            onChange={(event) => setForm((prev) => ({ ...prev, baseUrl: event.target.value }))}
            placeholder="https://api.local.test"
          />
        </label>

        <label className="flex items-center gap-3 text-sm text-slate-200 md:col-span-2">
          <input
            type="checkbox"
            checked={form.isActive}
            onChange={(event) => setForm((prev) => ({ ...prev, isActive: event.target.checked }))}
            className="h-4 w-4 rounded border-slate-600 bg-slate-900 text-indigo-400"
          />
          Active
        </label>
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
