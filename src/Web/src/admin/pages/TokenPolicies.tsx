import { useEffect, useState } from "react";
import { AppError, apiRequest } from "../api/http";
import type { AdminTokenPolicyConfig, AdminTokenPolicyValues } from "../api/types";
import { Field, FormError } from "../components/Field";
import { pushToast } from "../components/toast";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

const defaultPolicy: AdminTokenPolicyValues = {
  accessTokenMinutes: 30,
  identityTokenMinutes: 15,
  authorizationCodeMinutes: 5,
  refreshTokenDays: 30,
  refreshRotationEnabled: true,
  refreshReuseDetectionEnabled: true,
  refreshReuseLeewaySeconds: 30,
  clockSkewSeconds: 60,
};

export default function TokenPolicies() {
  const [policy, setPolicy] = useState<AdminTokenPolicyValues>(defaultPolicy);
  const [guardrails, setGuardrails] = useState<AdminTokenPolicyConfig["guardrails"] | null>(null);
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
        const response = await apiRequest<AdminTokenPolicyConfig>("/admin/api/security/token-policy");
        setPolicy(response.policy);
        setGuardrails(response.guardrails);
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
      const updated = await apiRequest<AdminTokenPolicyConfig>("/admin/api/security/token-policy", {
        method: "PUT",
        body: JSON.stringify(policy),
        suppressToast: true,
      });
      setPolicy(updated.policy);
      setGuardrails(updated.guardrails);
      pushToast({ tone: "success", message: "Token policy updated." });
    } catch (error) {
      const parsed = parseProblemDetailsErrors(error);
      setFormError(parsed.generalError);
      setFormDiagnostics(parsed.diagnostics);
      setFieldErrors(parsed.fieldErrors ?? {});
      if (!(error instanceof AppError)) {
        pushToast({ tone: "error", message: "Failed to update token policy." });
      }
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div className="text-sm text-slate-400">Loading token policy…</div>;
  }

  const guardrailHint = guardrails
    ? `Guardrails: ${guardrails.minAccessTokenMinutes}-${guardrails.maxAccessTokenMinutes} min access tokens, ${guardrails.minRefreshTokenDays}-${guardrails.maxRefreshTokenDays} day refresh.`
    : undefined;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">Token Policies</h1>
        <p className="mt-1 text-sm text-slate-400">
          Set global token lifetimes and refresh token safeguards for OpenID Connect flows.
        </p>
      </div>

      {!policy.refreshReuseDetectionEnabled && (
        <div className="rounded-lg border border-amber-500/40 bg-amber-500/10 p-4 text-sm text-amber-200">
          Refresh token reuse detection is disabled. Enable it to detect replayed refresh tokens.
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-2">
        <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-5">
          <h2 className="text-lg font-semibold text-white">Token lifetimes</h2>
          <p className="mt-1 text-sm text-slate-400">{guardrailHint ?? "Adjust lifetimes with care."}</p>
          <div className="mt-4 grid gap-4">
            <Field
              label="Access token (minutes)"
              tooltip="Recommended 5–60 minutes. Shorter values reduce exposure for leaked tokens."
              error={fieldErrors?.["accessTokenMinutes"]?.[0]}
            >
              <input
                type="number"
                min={1}
                value={policy.accessTokenMinutes}
                onChange={(event) =>
                  setPolicy((current) => ({
                    ...current,
                    accessTokenMinutes: Number(event.target.value),
                  }))
                }
                className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
              />
            </Field>
            <Field
              label="ID token (minutes)"
              tooltip="Keep short, usually aligned with access token lifetime."
              error={fieldErrors?.["identityTokenMinutes"]?.[0]}
            >
              <input
                type="number"
                min={1}
                value={policy.identityTokenMinutes}
                onChange={(event) =>
                  setPolicy((current) => ({
                    ...current,
                    identityTokenMinutes: Number(event.target.value),
                  }))
                }
                className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
              />
            </Field>
            <Field
              label="Authorization code (minutes)"
              tooltip="Short lifetimes reduce replay risk for authorization codes."
              error={fieldErrors?.["authorizationCodeMinutes"]?.[0]}
            >
              <input
                type="number"
                min={1}
                value={policy.authorizationCodeMinutes}
                onChange={(event) =>
                  setPolicy((current) => ({
                    ...current,
                    authorizationCodeMinutes: Number(event.target.value),
                  }))
                }
                className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
              />
            </Field>
            <Field
              label="Refresh token lifetime (days)"
              tooltip="Total lifetime for refresh tokens before re-authentication is required."
              error={fieldErrors?.["refreshTokenDays"]?.[0]}
            >
              <input
                type="number"
                min={1}
                value={policy.refreshTokenDays}
                onChange={(event) =>
                  setPolicy((current) => ({
                    ...current,
                    refreshTokenDays: Number(event.target.value),
                  }))
                }
                className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
              />
            </Field>
            <Field
              label="Clock skew (seconds)"
              tooltip="Allowed clock drift when validating tokens."
              error={fieldErrors?.["clockSkewSeconds"]?.[0]}
            >
              <input
                type="number"
                min={0}
                value={policy.clockSkewSeconds}
                onChange={(event) =>
                  setPolicy((current) => ({
                    ...current,
                    clockSkewSeconds: Number(event.target.value),
                  }))
                }
                className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
              />
            </Field>
          </div>
        </div>

        <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-5">
          <h2 className="text-lg font-semibold text-white">Refresh token protection</h2>
          <p className="mt-1 text-sm text-slate-400">
            Rotation and reuse detection reduce the impact of stolen refresh tokens.
          </p>
          <div className="mt-4 grid gap-4">
            <label className="flex items-center gap-3 text-sm text-slate-200">
              <input
                type="checkbox"
                checked={policy.refreshRotationEnabled}
                onChange={(event) =>
                  setPolicy((current) => ({
                    ...current,
                    refreshRotationEnabled: event.target.checked,
                  }))
                }
                className="h-4 w-4 rounded border-slate-700 bg-slate-950 text-indigo-400"
              />
              Enable refresh token rotation
            </label>
            <label className="flex items-center gap-3 text-sm text-slate-200">
              <input
                type="checkbox"
                checked={policy.refreshReuseDetectionEnabled}
                onChange={(event) =>
                  setPolicy((current) => ({
                    ...current,
                    refreshReuseDetectionEnabled: event.target.checked,
                  }))
                }
                className="h-4 w-4 rounded border-slate-700 bg-slate-950 text-indigo-400"
              />
              Enable reuse detection
            </label>
            <Field
              label="Reuse leeway (seconds)"
              tooltip="Allows short overlap during parallel refresh calls before marking a token as reused."
              error={fieldErrors?.["refreshReuseLeewaySeconds"]?.[0]}
            >
              <input
                type="number"
                min={0}
                value={policy.refreshReuseLeewaySeconds}
                onChange={(event) =>
                  setPolicy((current) => ({
                    ...current,
                    refreshReuseLeewaySeconds: Number(event.target.value),
                  }))
                }
                className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
              />
            </Field>
          </div>
        </div>
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
