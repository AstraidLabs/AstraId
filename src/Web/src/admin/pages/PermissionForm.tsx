import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { AppError, apiRequest } from "../api/http";
import type { AdminPermissionItem } from "../api/types";
import { Field, FormError } from "../components/Field";
import { pushToast } from "../components/toast";
import { toAdminRoute } from "../../routing";
import {
  validatePermissionDescription,
  validatePermissionGroup,
  validatePermissionKey,
} from "../validation/adminValidation";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

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
  const [errors, setErrors] = useState<{ key?: string; description?: string; group?: string }>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [formDiagnostics, setFormDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
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
    const keyValidation = validatePermissionKey(form.key);
    const descriptionValidation = validatePermissionDescription(form.description);
    const groupValidation = validatePermissionGroup(form.group);
    const nextErrors: { key?: string; description?: string; group?: string } = {
      key: keyValidation.error ?? undefined,
      description: descriptionValidation.error ?? undefined,
      group: groupValidation.error ?? undefined,
    };
    setErrors(nextErrors);
    setFormError(null);
    setFormDiagnostics(undefined);
    if (Object.values(nextErrors).some(Boolean)) {
      return;
    }

    setSaving(true);
    try {
      const payload = {
        key: keyValidation.value,
        description: descriptionValidation.value,
        group: groupValidation.value,
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
      if (error instanceof AppError) {
        const parsed = parseProblemDetailsErrors(error);
        setErrors({
          key: parsed.fieldErrors.key?.[0],
          description: parsed.fieldErrors.description?.[0],
          group: parsed.fieldErrors.group?.[0],
        });
        setFormError(parsed.generalError ?? "Unable to save permission.");
        setFormDiagnostics(parsed.diagnostics);
        return;
      }
      setFormError("Unable to save permission.");
      setFormDiagnostics(undefined);
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

      <FormError message={formError} diagnostics={formDiagnostics} />

      <div className="grid gap-6 rounded-lg border border-slate-800 bg-slate-900/40 p-6 md:grid-cols-2">
        <Field
          label="Key"
          tooltip="Jednoznačný identifikátor permission. Používej tečky pro hierarchii a lowercase."
          hint="Příklad: system.admin nebo api.read. Používá se v policy a claimu permission."
          error={errors.key}
          required
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
                key: validatePermissionKey(form.key).error ?? undefined,
              }))
            }
            placeholder="system.admin"
            disabled={mode === "edit" && form.isSystem}
          />
        </Field>

        <Field
          label="Group"
          tooltip="Skupina pro vizuální seskupení permission."
          hint="Volitelné, např. System nebo OIDC."
          error={errors.group}
        >
          <input
            className={`rounded-md border bg-slate-950 px-3 py-2 text-slate-100 ${
              errors.group ? "border-rose-400" : "border-slate-700"
            }`}
            value={form.group}
            onChange={(event) => setForm((prev) => ({ ...prev, group: event.target.value }))}
            onBlur={() =>
              setErrors((current) => ({
                ...current,
                group: validatePermissionGroup(form.group).error ?? undefined,
              }))
            }
            placeholder="System"
          />
        </Field>

        <div className="md:col-span-2">
          <Field
            label="Description"
            tooltip="Krátký popis pro administrátory."
            hint="Povinné."
            error={errors.description}
            required
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
                  description: validatePermissionDescription(form.description).error ?? undefined,
                }))
              }
              placeholder="Allows access to admin features"
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
