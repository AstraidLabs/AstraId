import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { AppError, apiRequest } from "../api/http";
import type { AdminOidcResourceDetail } from "../api/types";
import { Field, FormError } from "../components/Field";
import { pushToast } from "../components/toast";
import { toAdminRoute } from "../../routing";
import { validateResourceName } from "../validation/oidcValidation";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

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
  const [formError, setFormError] = useState<string | null>(null);
  const [formDiagnostics, setFormDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
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
    const validation = validateResourceName(form.name);
    if (validation.error) {
      setErrors({ name: validation.error });
      return;
    }
    setErrors({});
    setFormError(null);
    setFormDiagnostics(undefined);
    setSaving(true);
    try {
      const payload = {
        name: validation.value,
        displayName: form.displayName.trim() || null,
        description: form.description.trim() || null,
        isActive: form.isActive,
      };

      if (mode === "create") {
        const created = await apiRequest<AdminOidcResourceDetail>("/admin/api/oidc/resources", {
          method: "POST",
          body: JSON.stringify(payload),
          suppressToast: true,
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
        suppressToast: true,
      });
      pushToast({ message: "Resource updated.", tone: "success" });
    } catch (error) {
      if (error instanceof AppError) {
        const parsed = parseProblemDetailsErrors(error);
        setErrors({
          name: parsed.fieldErrors.name?.[0],
        });
        setFormError(parsed.generalError ?? "Unable to save resource.");
        setFormDiagnostics(parsed.diagnostics);
        return;
      }
      setFormError("Unable to save resource.");
      setFormDiagnostics(undefined);
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

      <FormError message={formError} diagnostics={formDiagnostics} />

      <div className="grid gap-6 rounded-lg border border-slate-800 bg-slate-900/40 p-6 md:grid-cols-2">
        <Field
          label="Name"
          tooltip="Resource reprezentuje API/audienci. Příklad: api, cms. Scopes na něj budou ukazovat."
          hint="3–100 znaků, lowercase bez mezer."
          error={errors.name}
          required
        >
          <input
            className={`rounded-md border bg-slate-950 px-3 py-2 text-slate-100 ${
              errors.name ? "border-rose-400" : "border-slate-700"
            }`}
            value={form.name}
            onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))}
            onBlur={() => {
              const validation = validateResourceName(form.name);
              setErrors((current) => ({ ...current, name: validation.error ?? undefined }));
            }}
            placeholder="api"
          />
        </Field>

        <Field
          label="Display name"
          tooltip="Lidský název resource pro admin přehledy."
          hint="Volitelné."
        >
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            value={form.displayName}
            onChange={(event) => setForm((prev) => ({ ...prev, displayName: event.target.value }))}
            placeholder="Core API"
          />
        </Field>

        <div className="md:col-span-2">
          <Field
            label="Description"
            tooltip="Interní popis resource pro administrátory."
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
            label="Status"
            tooltip="Deaktivovaný resource nelze přiřazovat ke scope, ale existující vazby zůstávají."
            hint="Vypni, pokud resource dočasně nepoužíváš."
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
              Active (available for scope selection)
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
