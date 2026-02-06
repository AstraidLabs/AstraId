import { useEffect, useState } from "react";
import { AppError, apiRequest } from "../api/http";
import type { AdminKeyRotationPolicyRequest, AdminKeyRotationPolicyResponse } from "../api/types";
import { Field, FormError } from "../components/Field";
import { pushToast } from "../components/toast";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

const defaultRequest: AdminKeyRotationPolicyRequest = {
  enabled: true,
  rotationIntervalDays: 30,
  gracePeriodDays: 14,
  jwksCacheMarginMinutes: 60,
  breakGlass: false,
  reason: "",
};

export default function SecurityRotationPolicy() {
  const [policy, setPolicy] = useState<AdminKeyRotationPolicyResponse | null>(null);
  const [form, setForm] = useState<AdminKeyRotationPolicyRequest>(defaultRequest);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [formDiagnostics, setFormDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const response = await apiRequest<AdminKeyRotationPolicyResponse>(
          "/admin/api/security/key-rotation-policy"
        );
        setPolicy(response);
        setForm({
          enabled: response.policy.enabled,
          rotationIntervalDays: response.policy.rotationIntervalDays,
          gracePeriodDays: response.policy.gracePeriodDays,
          jwksCacheMarginMinutes: response.policy.jwksCacheMarginMinutes,
          breakGlass: false,
          reason: "",
        });
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const handleSave = async () => {
    setSaving(true);
    setFormError(null);
    setFormDiagnostics(undefined);
    setFieldErrors({});
    try {
      const updated = await apiRequest<AdminKeyRotationPolicyResponse>(
        "/admin/api/security/key-rotation-policy",
        {
          method: "PUT",
          body: JSON.stringify(form),
          suppressToast: true,
        }
      );
      setPolicy(updated);
      setForm((current) => ({
        ...current,
        breakGlass: false,
        reason: "",
      }));
      pushToast({ tone: "success", message: "Key rotation policy updated." });
    } catch (error) {
      const parsed = parseProblemDetailsErrors(error);
      setFormError(parsed.generalError);
      setFormDiagnostics(parsed.diagnostics);
      setFieldErrors(parsed.fieldErrors ?? {});
      if (!(error instanceof AppError)) {
        pushToast({ tone: "error", message: "Failed to update key rotation policy." });
      }
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div className="text-sm text-slate-400">Loading key rotation policy…</div>;
  }

  const guardrails = policy?.guardrails;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">Rotation Policy</h1>
        <p className="mt-1 text-sm text-slate-400">
          Control automatic signing key rotation schedule and grace period.
        </p>
      </div>

      {!form.enabled && (
        <div className="rounded-lg border border-rose-500/40 bg-rose-500/10 p-4 text-sm text-rose-200">
          Rotation is disabled. Only use break-glass mode when necessary.
        </div>
      )}

      <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-5">
        <div className="grid gap-4 md:grid-cols-2">
          <Field
            label="Rotation interval (days)"
            tooltip={
              guardrails
                ? `Guardrails: ${guardrails.minRotationIntervalDays}–${guardrails.maxRotationIntervalDays} days.`
                : "Rotation cadence in days."
            }
            error={fieldErrors?.["rotationIntervalDays"]?.[0]}
          >
            <input
              type="number"
              min={1}
              value={form.rotationIntervalDays}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  rotationIntervalDays: Number(event.target.value),
                }))
              }
              className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
            />
          </Field>
          <Field
            label="Grace period (days)"
            tooltip={
              guardrails
                ? `Guardrails: ${guardrails.minGracePeriodDays}–${guardrails.maxGracePeriodDays} days.`
                : "How long previous keys remain published."
            }
            error={fieldErrors?.["gracePeriodDays"]?.[0]}
          >
            <input
              type="number"
              min={0}
              value={form.gracePeriodDays}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  gracePeriodDays: Number(event.target.value),
                }))
              }
              className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
            />
          </Field>
          <Field
            label="JWKS cache margin (minutes)"
            tooltip={
              guardrails
                ? `Guardrails: ${guardrails.minJwksCacheMarginMinutes}–${guardrails.maxJwksCacheMarginMinutes} minutes.`
                : "Extra time added to the safe rollover window for JWKS caches."
            }
            error={fieldErrors?.["jwksCacheMarginMinutes"]?.[0]}
          >
            <input
              type="number"
              min={0}
              value={form.jwksCacheMarginMinutes}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  jwksCacheMarginMinutes: Number(event.target.value),
                }))
              }
              className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
            />
          </Field>
        </div>

        <label className="mt-4 flex items-center gap-3 text-sm text-slate-200">
          <input
            type="checkbox"
            checked={form.enabled}
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                enabled: event.target.checked,
              }))
            }
            className="h-4 w-4 rounded border-slate-700 bg-slate-950 text-indigo-400"
          />
          Enable automatic rotation
        </label>

        {!form.enabled && (
          <div className="mt-4 grid gap-4">
            <label className="flex items-center gap-3 text-sm text-slate-200">
              <input
                type="checkbox"
                checked={form.breakGlass}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    breakGlass: event.target.checked,
                  }))
                }
                className="h-4 w-4 rounded border-slate-700 bg-slate-950 text-rose-400"
              />
              Break-glass confirmation
            </label>
            <Field
              label="Reason"
              tooltip="Provide a reason when disabling rotation in production."
              error={fieldErrors?.["reason"]?.[0]}
            >
              <input
                type="text"
                value={form.reason ?? ""}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    reason: event.target.value,
                  }))
                }
                className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
              />
            </Field>
          </div>
        )}
      </div>

      <FormError message={formError} diagnostics={formDiagnostics} />

      <div className="flex justify-end">
        <button
          type="button"
          onClick={handleSave}
          disabled={saving}
          className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60"
        >
          Save policy
        </button>
      </div>
    </div>
  );
}
