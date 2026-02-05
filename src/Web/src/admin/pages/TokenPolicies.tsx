import { useEffect, useMemo, useState } from "react";
import { AppError, apiRequest } from "../api/http";
import type { AdminTokenPolicyConfig } from "../api/types";
import { Field, FormError } from "../components/Field";
import { pushToast } from "../components/toast";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

type Tab = "public" | "confidential";

const defaultConfig: AdminTokenPolicyConfig = {
  public: {
    accessTokenMinutes: 30,
    identityTokenMinutes: 15,
    refreshTokenAbsoluteDays: 30,
    refreshTokenSlidingDays: 7,
  },
  confidential: {
    accessTokenMinutes: 60,
    identityTokenMinutes: 30,
    refreshTokenAbsoluteDays: 60,
    refreshTokenSlidingDays: 14,
  },
  refreshPolicy: {
    rotationEnabled: true,
    reuseDetectionEnabled: true,
    reuseLeewaySeconds: 30,
  },
};

const tabButtonClass = (active: boolean) =>
  `rounded-md px-3 py-2 text-sm font-semibold transition ${
    active ? "bg-slate-800 text-white" : "text-slate-300 hover:bg-slate-900"
  }`;

export default function TokenPolicies() {
  const [tab, setTab] = useState<Tab>("public");
  const [config, setConfig] = useState<AdminTokenPolicyConfig>(defaultConfig);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [formDiagnostics, setFormDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  const preset = useMemo(() => (tab === "public" ? config.public : config.confidential), [
    tab,
    config,
  ]);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const response = await apiRequest<AdminTokenPolicyConfig>("/admin/api/tokens/config");
        setConfig(response);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const updatePreset = (field: keyof AdminTokenPolicyConfig["public"], value: number) => {
    setConfig((current) => ({
      ...current,
      [tab]: {
        ...current[tab],
        [field]: value,
      },
    }));
  };

  const updateRefreshPolicy = (
    field: keyof AdminTokenPolicyConfig["refreshPolicy"],
    value: boolean | number
  ) => {
    setConfig((current) => ({
      ...current,
      refreshPolicy: {
        ...current.refreshPolicy,
        [field]: value,
      },
    }));
  };

  const handleSave = async () => {
    setSaving(true);
    setFormError(null);
    setFormDiagnostics(undefined);
    setFieldErrors({});
    try {
      const updated = await apiRequest<AdminTokenPolicyConfig>("/admin/api/tokens/config", {
        method: "PUT",
        body: JSON.stringify(config),
        suppressToast: true,
      });
      setConfig(updated);
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

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">Token Policies</h1>
        <p className="mt-1 text-sm text-slate-400">
          Tune token lifetimes and refresh token safeguards for public and confidential clients.
        </p>
      </div>

      <div className="flex gap-2 rounded-lg border border-slate-800 bg-slate-950/60 p-2">
        <button
          type="button"
          className={tabButtonClass(tab === "public")}
          onClick={() => setTab("public")}
        >
          Public clients
        </button>
        <button
          type="button"
          className={tabButtonClass(tab === "confidential")}
          onClick={() => setTab("confidential")}
        >
          Confidential clients
        </button>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-5">
          <h2 className="text-lg font-semibold text-white">Token lifetimes</h2>
          <p className="mt-1 text-sm text-slate-400">
            Shorter lifetimes improve security but increase refresh frequency.
          </p>
          <div className="mt-4 grid gap-4">
            <Field
              label="Access token (minutes)"
              tooltip="Recommended 5–60 minutes. Shorter values reduce exposure for leaked tokens."
              error={fieldErrors?.[`${tab}.accessTokenMinutes`]?.[0]}
            >
              <input
                type="number"
                min={1}
                value={preset.accessTokenMinutes}
                onChange={(event) => updatePreset("accessTokenMinutes", Number(event.target.value))}
                className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
              />
            </Field>
            <Field
              label="ID token (minutes)"
              tooltip="Keep short, usually aligned with access token lifetime."
              error={fieldErrors?.[`${tab}.identityTokenMinutes`]?.[0]}
            >
              <input
                type="number"
                min={1}
                value={preset.identityTokenMinutes}
                onChange={(event) => updatePreset("identityTokenMinutes", Number(event.target.value))}
                className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
              />
            </Field>
            <Field
              label="Refresh token absolute (days)"
              tooltip="Maximum lifetime regardless of activity."
              error={fieldErrors?.[`${tab}.refreshTokenAbsoluteDays`]?.[0]}
            >
              <input
                type="number"
                min={1}
                value={preset.refreshTokenAbsoluteDays}
                onChange={(event) =>
                  updatePreset("refreshTokenAbsoluteDays", Number(event.target.value))
                }
                className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
              />
            </Field>
            <Field
              label="Refresh token sliding (days)"
              tooltip="Extends refresh token lifetime while the client is active. Set to 0 to disable."
              error={fieldErrors?.[`${tab}.refreshTokenSlidingDays`]?.[0]}
            >
              <input
                type="number"
                min={0}
                value={preset.refreshTokenSlidingDays}
                onChange={(event) =>
                  updatePreset("refreshTokenSlidingDays", Number(event.target.value))
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
                checked={config.refreshPolicy.rotationEnabled}
                onChange={(event) => updateRefreshPolicy("rotationEnabled", event.target.checked)}
                className="h-4 w-4 rounded border-slate-700 bg-slate-950 text-indigo-400"
              />
              Enable refresh token rotation
            </label>
            <label className="flex items-center gap-3 text-sm text-slate-200">
              <input
                type="checkbox"
                checked={config.refreshPolicy.reuseDetectionEnabled}
                onChange={(event) =>
                  updateRefreshPolicy("reuseDetectionEnabled", event.target.checked)
                }
                className="h-4 w-4 rounded border-slate-700 bg-slate-950 text-indigo-400"
              />
              Enable reuse detection
            </label>
            <Field
              label="Reuse leeway (seconds)"
              tooltip="Allows short overlap during parallel refresh calls before marking a token as reused."
              error={fieldErrors?.["refreshPolicy.reuseLeewaySeconds"]?.[0]}
            >
              <input
                type="number"
                min={0}
                value={config.refreshPolicy.reuseLeewaySeconds}
                onChange={(event) =>
                  updateRefreshPolicy("reuseLeewaySeconds", Number(event.target.value))
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
