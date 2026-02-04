import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, apiRequest } from "../api/http";
import type {
  AdminOidcResourceListItem,
  AdminOidcScopeDetail,
  PagedResult,
} from "../api/types";
import { Field } from "../components/Field";
import { pushToast } from "../components/toast";
import { toAdminRoute } from "../../routing";
import { validateScopeName } from "../validation/oidcValidation";

type Mode = "create" | "edit";

type Props = {
  mode: Mode;
  scopeId?: string;
};

type FormState = {
  name: string;
  displayName: string;
  description: string;
  resources: string[];
  claimsText: string;
};

const defaultState: FormState = {
  name: "",
  displayName: "",
  description: "",
  resources: [],
  claimsText: "",
};

export default function OidcScopeForm({ mode, scopeId }: Props) {
  const navigate = useNavigate();
  const [form, setForm] = useState<FormState>(defaultState);
  const [resources, setResources] = useState<AdminOidcResourceListItem[]>([]);
  const [errors, setErrors] = useState<{ name?: string; resources?: string }>({});
  const [loading, setLoading] = useState(mode === "edit");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const fetchResources = async () => {
      const params = new URLSearchParams({
        page: "1",
        pageSize: "200",
        includeInactive: "false",
      });
      const data = await apiRequest<PagedResult<AdminOidcResourceListItem>>(
        `/admin/api/oidc/resources?${params.toString()}`
      );
      setResources(data.items);
    };
    fetchResources();
  }, []);

  useEffect(() => {
    if (mode !== "edit" || !scopeId) {
      return;
    }

    let isMounted = true;
    const fetchScope = async () => {
      setLoading(true);
      try {
        const data = await apiRequest<AdminOidcScopeDetail>(`/admin/api/oidc/scopes/${scopeId}`);
        if (!isMounted) {
          return;
        }
        setForm({
          name: data.name,
          displayName: data.displayName ?? "",
          description: data.description ?? "",
          resources: data.resources,
          claimsText: data.claims.join("\n"),
        });
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    fetchScope();
    return () => {
      isMounted = false;
    };
  }, [mode, scopeId]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    const validation = validateScopeName(form.name);
    if (validation.error) {
      setErrors({ name: validation.error });
      return;
    }
    setErrors({});
    setSaving(true);
    try {
      const payload = {
        name: validation.value,
        displayName: form.displayName.trim() || null,
        description: form.description.trim() || null,
        resources: form.resources,
        claims: splitLines(form.claimsText),
      };

      if (mode === "create") {
        const created = await apiRequest<AdminOidcScopeDetail>("/admin/api/oidc/scopes", {
          method: "POST",
          body: JSON.stringify(payload),
          suppressToast: true,
        });
        pushToast({ message: "Scope created.", tone: "success" });
        navigate(toAdminRoute(`/oidc/scopes/${created.id}`));
        return;
      }

      if (!scopeId) {
        return;
      }

      await apiRequest<AdminOidcScopeDetail>(`/admin/api/oidc/scopes/${scopeId}`, {
        method: "PUT",
        body: JSON.stringify(payload),
        suppressToast: true,
      });
      pushToast({ message: "Scope updated.", tone: "success" });
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.fieldErrors) {
          setErrors((current) => ({
            ...current,
            name: error.fieldErrors.name?.[0] ?? current.name,
            resources: error.fieldErrors.resources?.[0] ?? current.resources,
          }));
          return;
        }
        pushToast({ message: error.message, tone: "error" });
        return;
      }
      pushToast({ message: "Unable to save scope.", tone: "error" });
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6 text-slate-300">
        Loading scope...
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">
          {mode === "create" ? "Create scope" : "Edit scope"}
        </h1>
        <p className="text-sm text-slate-300">Define OpenIddict scopes and their resources.</p>
      </div>

      <div className="grid gap-6 rounded-lg border border-slate-800 bg-slate-900/40 p-6 md:grid-cols-2">
        <Field
          label="Name"
          tooltip="OIDC scope identifikátor. Používej lowercase, bez mezer. Příklad: api, api.read, cms:write."
          hint="3–100 znaků, povoleno: a-z, 0-9, :, ., _, -."
          error={errors.name}
        >
          <input
            className={`rounded-md border bg-slate-950 px-3 py-2 text-slate-100 ${
              errors.name ? "border-rose-400" : "border-slate-700"
            }`}
            value={form.name}
            onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))}
            onBlur={() => {
              const validation = validateScopeName(form.name);
              setErrors((current) => ({ ...current, name: validation.error ?? undefined }));
            }}
            placeholder="api.read"
          />
        </Field>

        <Field
          label="Display name"
          tooltip="Přátelský název scope pro admin přehledy. Neovlivňuje protokol."
          hint="Volitelné."
        >
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            value={form.displayName}
            onChange={(event) => setForm((prev) => ({ ...prev, displayName: event.target.value }))}
            placeholder="API Read"
          />
        </Field>

        <div className="md:col-span-2">
          <Field
            label="Description"
            tooltip="Interní popis scope pro administrátory."
            hint="Volitelné."
          >
            <textarea
              className="min-h-[120px] rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              value={form.description}
              onChange={(event) => setForm((prev) => ({ ...prev, description: event.target.value }))}
              placeholder="Optional description for admins."
            />
          </Field>
        </div>

        <div className="md:col-span-2">
          <Field
            label="Resources"
            tooltip="Resources reprezentují API/audienci. Scope může mapovat na více resources."
            hint="Vyber resources, které tento scope chrání."
            error={errors.resources}
          >
            <div className="grid gap-2 md:grid-cols-2">
              {resources.length === 0 && (
                <div className="rounded-md border border-dashed border-slate-700 bg-slate-950/40 p-4 text-sm text-slate-400">
                  No active resources yet — create one.
                </div>
              )}
              {resources.map((resource) => (
                <label
                  key={resource.id}
                  className="flex items-center gap-3 rounded-md border border-slate-800 bg-slate-950/40 p-3"
                >
                  <input
                    type="checkbox"
                    checked={form.resources.includes(resource.name)}
                    onChange={(event) => {
                      const next = new Set(form.resources);
                      if (event.target.checked) {
                        next.add(resource.name);
                      } else {
                        next.delete(resource.name);
                      }
                      setForm((prev) => ({ ...prev, resources: Array.from(next) }));
                    }}
                    className="h-4 w-4 rounded border-slate-600 bg-slate-900 text-indigo-400"
                  />
                  <div>
                    <div className="text-sm font-semibold text-slate-100">{resource.name}</div>
                    <div className="text-xs text-slate-400">
                      {resource.displayName ?? "No display name"}
                    </div>
                  </div>
                </label>
              ))}
            </div>
          </Field>
        </div>

        <div className="md:col-span-2">
          <Field
            label="Claims"
            tooltip="Volitelné claimy přidávané do tokenu, jeden claim na řádek."
            hint="1 claim per line, např. preferred_username."
          >
            <textarea
              className="min-h-[120px] rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              value={form.claimsText}
              onChange={(event) => setForm((prev) => ({ ...prev, claimsText: event.target.value }))}
              placeholder="custom_claim"
            />
          </Field>
        </div>
      </div>

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={saving}
          className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60"
        >
          {saving ? "Saving..." : "Save scope"}
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

function splitLines(value: string) {
  return value
    .split("\n")
    .map((line) => line.trim())
    .filter(Boolean);
}
