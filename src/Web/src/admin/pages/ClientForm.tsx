import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { AlertTriangle, CircleAlert, Info, OctagonX, ShieldAlert } from "lucide-react";
import { AppError, apiRequest } from "../api/http";
import type {
  AdminClientDetail,
  AdminClientSecretResponse,
  AdminClientPresetListItem,
  AdminClientPresetDetail,
  AdminClientProfileRulesResponse,
  AdminOidcScopeListItem,
  PagedResult,
  AdminFeaturesResponse,
  AdminClientPreviewResponse,
  AdminClientSaveResponse,
  FindingDto,
} from "../api/types";
import { Field, FormError } from "../components/Field";
import MultiLineListInput from "../components/MultiLineListInput";
import { pushToast } from "../components/toast";
import { toAdminRoute } from "../../routing";
import {
  getRedirectErrorMessage,
  parseRedirectUris,
  type RedirectUriError,
  validateClientId,
  validateGrantTypes,
  validatePkceRules,
  validateRedirectUris,
} from "../validation/oidcValidation";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

type Mode = "create" | "edit";

type Props = {
  mode: Mode;
  clientId?: string;
};

type FormState = {
  clientId: string;
  displayName: string;
  clientType: "public" | "confidential";
  enabled: boolean;
  grantTypes: string[];
  pkceRequired: boolean;
  clientApplicationType: "web" | "mobile" | "desktop" | "integration";
  allowIntrospection: boolean;
  allowUserCredentials: boolean;
  allowedScopesForPasswordGrant: string[];
  scopes: string[];
  redirectUrisText: string;
  postLogoutRedirectUrisText: string;
  brandingDisplayName: string;
  brandingLogoUrl: string;
  brandingHomeUrl: string;
  brandingPrivacyUrl: string;
  brandingTermsUrl: string;
};

type FormErrors = {
  clientId?: string;
  presetId?: string;
  grantTypes?: string;
  pkceRequired?: string;
  redirectUris?: string;
  postLogoutRedirectUris?: string;
  scopes?: string;
  clientApplicationType?: string;
  allowUserCredentials?: string;
  allowedScopesForPasswordGrant?: string;
  brandingLogoUrl?: string;
  brandingHomeUrl?: string;
  brandingPrivacyUrl?: string;
  brandingTermsUrl?: string;
};

const defaultState: FormState = {
  clientId: "",
  displayName: "",
  clientType: "public",
  enabled: true,
  grantTypes: ["authorization_code", "refresh_token"],
  pkceRequired: true,
  clientApplicationType: "web",
  allowIntrospection: false,
  allowUserCredentials: false,
  allowedScopesForPasswordGrant: [],
  scopes: [],
  redirectUrisText: "",
  postLogoutRedirectUrisText: "",
  brandingDisplayName: "",
  brandingLogoUrl: "",
  brandingHomeUrl: "",
  brandingPrivacyUrl: "",
  brandingTermsUrl: "",
};

export default function ClientForm({ mode, clientId }: Props) {
  const navigate = useNavigate();
  const location = useLocation();
  const [form, setForm] = useState<FormState>(defaultState);
  const [scopes, setScopes] = useState<AdminOidcScopeListItem[]>([]);
  const [loading, setLoading] = useState(mode === "edit");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<FormErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [formDiagnostics, setFormDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
  const [redirectErrors, setRedirectErrors] = useState<RedirectUriError[]>([]);
  const [postLogoutErrors, setPostLogoutErrors] = useState<RedirectUriError[]>([]);
  const [secret, setSecret] = useState<string | null>(() => {
    const state = location.state as { secret?: string } | null;
    return state?.secret ?? null;
  });
  const [showSecret, setShowSecret] = useState(() => Boolean(secret));
  const [secretNotice, setSecretNotice] = useState<string | null>(null);
  const [profiles, setProfiles] = useState<AdminClientProfileRulesResponse["profiles"]>([]);
  const [presets, setPresets] = useState<AdminClientPresetListItem[]>([]);
  const [presetId, setPresetId] = useState<string>("");
  const [presetDetail, setPresetDetail] = useState<AdminClientPresetDetail | null>(null);
  const [profile, setProfile] = useState<string>("SpaPublic");
  const [enablePasswordGrant, setEnablePasswordGrant] = useState(false);
  const [findings, setFindings] = useState<FindingDto[]>([]);
  const [previewBusy, setPreviewBusy] = useState(false);
  const previewAbortRef = useRef<AbortController | null>(null);

  const isConfidential = form.clientType === "confidential";
  const activeProfileRule = useMemo(() => profiles.find((item) => item.profile === profile), [profiles, profile]);
  const severityOrder = ["error", "risk", "warning", "deprecated", "info"] as const;
  const severityCounts = useMemo(() => {
    const counts: Record<string, number> = { error: 0, risk: 0, warning: 0, deprecated: 0, info: 0 };
    findings.forEach((finding) => { counts[finding.severity] = (counts[finding.severity] ?? 0) + 1; });
    return counts;
  }, [findings]);
  const blockingFindings = useMemo(() => findings.filter((finding) => finding.severity === "error"), [findings]);
  const findingsByField = useMemo(() => findings.reduce<Record<string, FindingDto[]>>((acc, finding) => {
    if (!finding.field) return acc;
    acc[finding.field] = [...(acc[finding.field] ?? []), finding];
    return acc;
  }, {}), [findings]);

  const grantTypeOptions = useMemo(
    () => [
      { value: "authorization_code", label: "Authorization Code" },
      { value: "refresh_token", label: "Refresh Token" },
      { value: "client_credentials", label: "Client Credentials" },
    ],
    []
  );

  useEffect(() => {
    const fetchScopes = async () => {
      const params = new URLSearchParams({ page: "1", pageSize: "200" });
      const data = await apiRequest<PagedResult<AdminOidcScopeListItem>>(
        `/admin/api/oidc/scopes?${params.toString()}`
      );
      setScopes(data.items);
    };
    fetchScopes();
    const fetchRules = async () => {
      const rules = await apiRequest<AdminClientProfileRulesResponse>("/admin/api/client-profiles/rules");
      const presetList = await apiRequest<AdminClientPresetListItem[]>("/admin/api/client-presets");
      const features = await apiRequest<AdminFeaturesResponse>("/admin/api/features");
      setProfiles(rules.profiles);
      setEnablePasswordGrant(features.enablePasswordGrant);
      setPresets(presetList);
      if (presetList.length > 0) {
        setPresetId(presetList[0].id);
        setProfile(presetList[0].profile);
      }
    };
    fetchRules();
  }, []);


  useEffect(() => {
    if (enablePasswordGrant) {
      return;
    }

    setForm((prev) => ({
      ...prev,
      grantTypes: prev.grantTypes.filter((grantType) => grantType !== "password"),
      allowUserCredentials: false,
      allowedScopesForPasswordGrant: [],
    }));
  }, [enablePasswordGrant]);

  useEffect(() => {
    if (!presetId) return;
    apiRequest<AdminClientPresetDetail>(`/admin/api/client-presets/${presetId}`).then((detail) => {
      setPresetDetail(detail);
      setProfile(detail.profile);
      if (mode === "create") {
        setForm((prev) => ({
          ...prev,
          clientType: detail.defaults.clientType === "confidential" ? "confidential" : "public",
          pkceRequired: detail.defaults.pkceRequired,
          grantTypes: detail.defaults.grantTypes,
          clientApplicationType: "web",
          allowIntrospection: false,
          allowUserCredentials: false,
          allowedScopesForPasswordGrant: [],
          redirectUrisText: detail.defaults.redirectUris.join("\n"),
          postLogoutRedirectUrisText: detail.defaults.postLogoutRedirectUris.join("\n"),
          scopes: detail.defaults.scopes,
        }));
      }
    });
  }, [presetId, mode]);

  useEffect(() => {
    if (mode !== "edit" || !clientId) {
      return;
    }

    let isMounted = true;
    const fetchClient = async () => {
      setLoading(true);
      try {
        const data = await apiRequest<AdminClientDetail>(`/admin/api/clients/${clientId}`);
        if (!isMounted) {
          return;
        }
        setForm({
          clientId: data.clientId,
          displayName: data.displayName ?? "",
          clientType: data.clientType.toLowerCase() === "confidential" ? "confidential" : "public",
          enabled: data.enabled,
          grantTypes: data.grantTypes,
          pkceRequired: data.pkceRequired,
          clientApplicationType: (data.clientApplicationType as FormState["clientApplicationType"]) ?? "web",
          allowIntrospection: data.allowIntrospection,
          allowUserCredentials: data.allowUserCredentials,
          allowedScopesForPasswordGrant: data.allowedScopesForPasswordGrant,
          scopes: data.scopes,
          redirectUrisText: data.redirectUris.join("\n"),
          postLogoutRedirectUrisText: data.postLogoutRedirectUris.join("\n"),
          brandingDisplayName: data.branding?.displayName ?? "",
          brandingLogoUrl: data.branding?.logoUrl ?? "",
          brandingHomeUrl: data.branding?.homeUrl ?? "",
          brandingPrivacyUrl: data.branding?.privacyUrl ?? "",
          brandingTermsUrl: data.branding?.termsUrl ?? "",
        });
        setProfile(data.profile ?? "SpaPublic");
        if (data.presetId) {
          setPresetId(data.presetId);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    fetchClient();
    return () => {
      isMounted = false;
    };
  }, [clientId, mode]);

  const severityMeta: Record<string, { icon: typeof AlertTriangle; color: string }> = {
    error: { icon: OctagonX, color: "text-rose-300" },
    risk: { icon: ShieldAlert, color: "text-amber-300" },
    warning: { icon: AlertTriangle, color: "text-yellow-300" },
    deprecated: { icon: CircleAlert, color: "text-orange-300" },
    info: { icon: Info, color: "text-sky-300" },
  };

  const renderInlineFindings = (field: string) => (
    <div className="mt-2 space-y-2">
      {(findingsByField[field] ?? []).map((finding) => {
        const meta = severityMeta[finding.severity] ?? severityMeta.info;
        const Icon = meta.icon;
        return (
          <div key={`${finding.code}-${finding.message}`} className="rounded-md border border-slate-700 bg-slate-950/80 p-2 text-xs text-slate-200">
            <p className="flex items-start gap-2"><Icon className={`h-4 w-4 ${meta.color}`} />{finding.title}: {finding.message}</p>
          </div>
        );
      })}
    </div>
  );

  const scrollToField = (field: string) => {
    const target = document.querySelector(`[data-lint-field="${field}"]`) as HTMLElement | null;
    if (!target) return;
    target.scrollIntoView({ behavior: "smooth", block: "center" });
    const focusable = target.querySelector("input, select, textarea") as HTMLElement | null;
    focusable?.focus();
  };

  const buildPayload = (currentForm: FormState) => ({
    clientId: currentForm.clientId.trim(),
    displayName: currentForm.displayName.trim() || null,
    clientType: currentForm.clientType,
    enabled: currentForm.enabled,
    grantTypes: currentForm.grantTypes,
    pkceRequired: currentForm.clientType === "public" ? currentForm.pkceRequired : false,
    clientApplicationType: currentForm.clientApplicationType,
    allowIntrospection: currentForm.allowIntrospection,
    allowUserCredentials: currentForm.allowUserCredentials,
    allowedScopesForPasswordGrant: currentForm.allowedScopesForPasswordGrant,
    scopes: currentForm.scopes,
    redirectUris: parseRedirectUris(currentForm.redirectUrisText),
    postLogoutRedirectUris: parseRedirectUris(currentForm.postLogoutRedirectUrisText),
    profile,
    presetId,
    presetVersion: presetDetail?.version,
    overrides: {
      clientType: currentForm.clientType,
      pkceRequired: currentForm.pkceRequired,
      grantTypes: currentForm.grantTypes,
      clientApplicationType: currentForm.clientApplicationType,
      allowIntrospection: currentForm.allowIntrospection,
      allowUserCredentials: currentForm.allowUserCredentials,
      allowedScopesForPasswordGrant: currentForm.allowedScopesForPasswordGrant,
      scopes: currentForm.scopes,
      redirectUris: parseRedirectUris(currentForm.redirectUrisText),
      postLogoutRedirectUris: parseRedirectUris(currentForm.postLogoutRedirectUrisText),
    },
    branding: {
      displayName: currentForm.brandingDisplayName.trim() || null,
      logoUrl: currentForm.brandingLogoUrl.trim() || null,
      homeUrl: currentForm.brandingHomeUrl.trim() || null,
      privacyUrl: currentForm.brandingPrivacyUrl.trim() || null,
      termsUrl: currentForm.brandingTermsUrl.trim() || null,
    },
  });

  useEffect(() => {
    if (!presetId) return;
    const timeout = window.setTimeout(async () => {
      previewAbortRef.current?.abort();
      const controller = new AbortController();
      previewAbortRef.current = controller;
      setPreviewBusy(true);
      try {
        const response = await apiRequest<AdminClientPreviewResponse>("/admin/api/clients/preview", {
          method: "POST",
          body: JSON.stringify(buildPayload(form)),
          signal: controller.signal,
          suppressToast: true,
        });
        setFindings(response.findings ?? []);
      } catch {
        // ignore transient preview failures
      } finally {
        setPreviewBusy(false);
      }
    }, 400);

    return () => window.clearTimeout(timeout);
  }, [form, presetId, presetDetail?.version, profile]);

  const runValidation = (nextForm: FormState) => {
    const nextErrors: FormErrors = {};
    const clientIdResult = validateClientId(nextForm.clientId);
    if (clientIdResult.error) {
      nextErrors.clientId = clientIdResult.error;
    }

    const grantError = validateGrantTypes(nextForm.grantTypes);
    if (grantError) {
      nextErrors.grantTypes = grantError;
    }

    const pkceError = validatePkceRules(
      nextForm.clientType,
      nextForm.grantTypes,
      nextForm.pkceRequired
    );
    if (pkceError) {
      nextErrors.pkceRequired = pkceError;
    }

    const redirectResult = validateRedirectUris(
      nextForm.redirectUrisText,
      nextForm.grantTypes.includes("authorization_code")
    );
    setRedirectErrors(redirectResult.errors);
    if (redirectResult.errors.length > 0) {
      nextErrors.redirectUris = getRedirectErrorMessage(redirectResult.errors) ?? undefined;
    }

    const postLogoutResult = validateRedirectUris(nextForm.postLogoutRedirectUrisText, false);
    setPostLogoutErrors(postLogoutResult.errors);
    if (postLogoutResult.errors.length > 0) {
      nextErrors.postLogoutRedirectUris = getRedirectErrorMessage(postLogoutResult.errors) ?? undefined;
    }


    const validateOptionalAbsoluteUrl = (value: string, field: keyof FormErrors, label: string) => {
      const normalized = value.trim();
      if (!normalized) return;
      try {
        const parsed = new URL(normalized);
        if (!/^https?:$/i.test(parsed.protocol)) {
          nextErrors[field] = `${label} must use HTTP(S).`;
        }
      } catch {
        nextErrors[field] = `${label} must be a valid absolute URL.`;
      }
    };

    validateOptionalAbsoluteUrl(nextForm.brandingLogoUrl, "brandingLogoUrl", "Logo URL");
    validateOptionalAbsoluteUrl(nextForm.brandingHomeUrl, "brandingHomeUrl", "Home URL");
    validateOptionalAbsoluteUrl(nextForm.brandingPrivacyUrl, "brandingPrivacyUrl", "Privacy URL");
    validateOptionalAbsoluteUrl(nextForm.brandingTermsUrl, "brandingTermsUrl", "Terms URL");

    if (!enablePasswordGrant && (nextForm.allowUserCredentials || nextForm.grantTypes.includes("password"))) {
      nextErrors.grantTypes = "Password grant is disabled by server feature flag.";
    }

    if (nextForm.allowUserCredentials) {
      if (nextForm.clientType !== "confidential" || nextForm.clientApplicationType !== "integration") {
        nextErrors.allowUserCredentials = "Password grant requires confidential integration client.";
      }
      if (!nextForm.grantTypes.includes("password")) {
        nextErrors.grantTypes = "Password grant must be enabled when allowUserCredentials is selected.";
      }
      if (nextForm.allowedScopesForPasswordGrant.length === 0) {
        nextErrors.allowedScopesForPasswordGrant = "Select at least one allowed scope for password grant.";
      }
    }

    setErrors(nextErrors);
    return nextErrors;
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    const validationErrors = runValidation(form);
    if (Object.keys(validationErrors).length > 0) {
      return;
    }
    setFormError(null);
    setFormDiagnostics(undefined);
    setSaving(true);
    try {
      const payload = buildPayload(form);

      if (mode === "create") {
        const response = await apiRequest<AdminClientSaveResponse>("/admin/api/clients", {
          method: "POST",
          body: JSON.stringify(payload),
          suppressToast: true,
        });
        setFindings(response.findings ?? []);
        if (response.clientSecret) {
          setSecret(response.clientSecret);
          setShowSecret(true);
          setSecretNotice(null);
        } else {
          setSecret(null);
          setShowSecret(false);
          setSecretNotice("Client created. Secret was not displayed for security reasons.");
        }
        pushToast({ message: "Client created.", tone: "success" });
        navigate(toAdminRoute(`/oidc/clients/${response.client.id}`), {
          state: { secret: response.clientSecret },
        });
        return;
      }

      if (!clientId) {
        return;
      }

      const response = await apiRequest<AdminClientSaveResponse>(`/admin/api/clients/${clientId}`, {
        method: "PUT",
        body: JSON.stringify(payload),
        suppressToast: true,
      });
      setFindings(response.findings ?? []);
      pushToast({ message: "Client updated.", tone: "success" });
    } catch (error) {
      if (error instanceof AppError) {
        const parsed = parseProblemDetailsErrors(error);
        setErrors({
          clientId: parsed.fieldErrors.clientId?.[0],
          grantTypes: parsed.fieldErrors.grantTypes?.[0],
          pkceRequired: parsed.fieldErrors.pkceRequired?.[0],
          redirectUris: parsed.fieldErrors.redirectUris?.[0],
          postLogoutRedirectUris: parsed.fieldErrors.postLogoutRedirectUris?.[0],
          scopes: parsed.fieldErrors.scopes?.[0],
          presetId: parsed.fieldErrors.presetId?.[0],
          clientApplicationType: parsed.fieldErrors.clientApplicationType?.[0],
          allowUserCredentials: parsed.fieldErrors.allowUserCredentials?.[0],
          allowedScopesForPasswordGrant: parsed.fieldErrors.allowedScopesForPasswordGrant?.[0],
          brandingLogoUrl: parsed.fieldErrors["branding.logoUrl"]?.[0],
          brandingHomeUrl: parsed.fieldErrors["branding.homeUrl"]?.[0],
          brandingPrivacyUrl: parsed.fieldErrors["branding.privacyUrl"]?.[0],
          brandingTermsUrl: parsed.fieldErrors["branding.termsUrl"]?.[0],
        });
        setFormError(parsed.generalError ?? "Unable to save client.");
        setFormDiagnostics(parsed.diagnostics);
        return;
      }
      setFormError("Unable to save client.");
      setFormDiagnostics(undefined);
    } finally {
      setSaving(false);
    }
  };

  const handleRotateSecret = async () => {
    if (!clientId) {
      return;
    }
    const response = await apiRequest<AdminClientSecretResponse>(
      `/admin/api/clients/${clientId}/rotate-secret`,
      { method: "POST" }
    );
    if (response.clientSecret) {
      setSecret(response.clientSecret);
      setShowSecret(true);
      setSecretNotice(null);
    } else {
      setSecret(null);
      setShowSecret(false);
      setSecretNotice("Secret rotated. New secret was not displayed for security reasons.");
    }
  };

  if (loading) {
    return (
      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6 text-slate-300">
        Loading client...
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-white">
            {mode === "create" ? "Create client" : "Edit client"}
          </h1>
          <p className="text-sm text-slate-300">
            Configure OpenIddict client settings and permissions.
          </p>
        </div>
        <div className="flex items-center gap-3">
          {mode === "edit" && isConfidential && (
            <button
              type="button"
              onClick={handleRotateSecret}
              className="rounded-md border border-amber-300/50 px-4 py-2 text-sm font-semibold text-amber-200 hover:bg-amber-400/10"
            >
              Rotate secret
            </button>
          )}
          <button
            type="submit"
            disabled={saving || blockingFindings.length > 0}
            className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60"
          >
            {saving ? "Saving..." : "Save"}
          </button>
        </div>
      </div>

      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
      <FormError message={formError} diagnostics={formDiagnostics} />
        <div className="mb-4 rounded-lg border border-slate-700 bg-slate-950/60 p-4">
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-semibold text-white">Configuration Health</h2>
            {previewBusy && <span className="text-xs text-slate-400">Analyzing...</span>}
          </div>
          <div className="mt-2 flex flex-wrap gap-2 text-xs text-slate-300">
            {severityOrder.map((severity) => (
              <span key={severity} className="rounded-full border border-slate-700 px-2 py-1">{severity}: {severityCounts[severity]}</span>
            ))}
          </div>
          {severityCounts.risk > 0 && <p className="mt-2 text-xs text-amber-300">Proceed with caution.</p>}
          <div className="mt-3 space-y-2">
            {findings.map((finding) => {
              const meta = severityMeta[finding.severity] ?? severityMeta.info;
              const Icon = meta.icon;
              return (
                <button
                  key={`${finding.code}-${finding.field ?? "global"}-${finding.message}`}
                  type="button"
                  className="w-full rounded-md border border-slate-700 bg-slate-950/80 p-2 text-left text-xs text-slate-200"
                  onClick={() => finding.field && scrollToField(finding.field)}
                >
                  <span className="flex items-start gap-2"><Icon className={`h-4 w-4 ${meta.color}`} />{finding.title}: {finding.message}</span>
                </button>
              );
            })}
          </div>
        </div>
        {mode === "create" && (
          <div className="mb-6 grid gap-5 md:grid-cols-2">
            <p className="md:col-span-2 text-xs uppercase tracking-wide text-indigo-300">Step 1: Profile → Step 2: Preset → Step 3: Configure</p>
            <Field label="Client profile" hint="Server-published profile rules.">
              <select
                className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
                value={profile}
                onChange={(event) => {
                  const nextProfile = event.target.value;
                  setProfile(nextProfile);
                  const matched = presets.find((item) => item.profile === nextProfile);
                  if (matched) {
                    setPresetId(matched.id);
                  }
                }}
              >
                {profiles.map((rule) => (
                  <option key={rule.profile} value={rule.profile}>{rule.profile}</option>
                ))}
              </select>
            </Field>
            <Field label="Preset" error={errors.presetId} hint="Preset defaults and locked fields are provided by server.">
              <select
                className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
                value={presetId}
                onChange={(event) => setPresetId(event.target.value)}
              >
                {presets.filter((item) => item.profile === profile).map((preset) => (
                  <option key={preset.id} value={preset.id}>{preset.name} (v{preset.version})</option>
                ))}
              </select>
            </Field>
          </div>
        )}
        {activeProfileRule && (
          <p className="mb-4 text-xs text-slate-400">{activeProfileRule.explanations.pkce ?? activeProfileRule.summary}</p>
        )}
        <div className="grid gap-5 md:grid-cols-2">
          <Field
            label="Client ID"
            tooltip="Identifikátor klienta v OIDC. Používej 3–100 znaků bez mezer. Příklad: web-spa, cms-admin."
            hint="Používej písmena, čísla, _, . nebo -. Lowercase doporučeno."
            error={errors.clientId}
            required
          >
            <input
              className={`rounded-md border bg-slate-950 px-3 py-2 text-slate-100 ${
                errors.clientId ? "border-rose-400" : "border-slate-700"
              }`}
              value={form.clientId}
              onChange={(event) =>
                setForm((prev) => ({ ...prev, clientId: event.target.value }))
              }
              onBlur={() => {
                const result = validateClientId(form.clientId);
                setErrors((current) => ({ ...current, clientId: result.error ?? undefined }));
              }}
              placeholder="web-spa"
              required
            />
          </Field>

          <Field
            label="Display name"
            tooltip="Lidský název klienta pro admin přehledy. Neovlivňuje protokolové hodnoty."
            hint="Volitelné, doporučeno pro lepší orientaci."
          >
            <input
              className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              value={form.displayName}
              onChange={(event) =>
                setForm((prev) => ({ ...prev, displayName: event.target.value }))
              }
              placeholder="CMS Admin SPA"
            />
          </Field>

          <Field
            label="Client type"
            tooltip="Public klient je SPA/mobile bez bezpečného úložiště tajemství. Confidential klient je server-to-server s uloženým secret."
            hint="Public = SPA/mobile. Confidential = backend."
          >
            <select
              className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              value={form.clientType}
              onChange={(event) => {
                const nextType = event.target.value as FormState["clientType"];
                setForm((prev) => ({ ...prev, clientType: nextType }));
                setErrors((current) => ({
                  ...current,
                  pkceRequired: undefined,
                  grantTypes: undefined,
                }));
              }}
              disabled={presetDetail?.lockedFields.includes("clientType")}
            >
              <option value="public">Public</option>
              <option value="confidential">Confidential</option>
            </select>
          </Field>

          <Field
            label="Status"
            tooltip="Zakázaní klienta zastaví autorizace i tokeny."
            hint="Vypni, pokud nechceš klienta dočasně používat."
          >
            <label className="flex items-center gap-3 text-sm text-slate-200">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-slate-600 bg-slate-950 text-indigo-500"
                checked={form.enabled}
                onChange={(event) =>
                  setForm((prev) => ({ ...prev, enabled: event.target.checked }))
                }
              />
              Enabled
            </label>
          </Field>

          <Field label="Client application type" error={errors.clientApplicationType}>
            <select
              className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              value={form.clientApplicationType}
              onChange={(event) => setForm((prev) => ({ ...prev, clientApplicationType: event.target.value as FormState["clientApplicationType"] }))}
            >
              <option value="web">Web</option>
              <option value="mobile">Mobile</option>
              <option value="desktop">Desktop</option>
              <option value="integration">Integration</option>
            </select>
          </Field>
        </div>
      </section>

      {(activeProfileRule?.sections.grants ?? true) && (
      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
          <div data-lint-field="grantTypes">
          <Field
            label="Grant types"
            tooltip="Grant types definují, jak klient získá token. Pro SPA typicky authorization_code + refresh_token."
            hint="Vyber alespoň jeden grant type."
            error={errors.grantTypes}
            required
          >
          <div className="flex flex-wrap gap-4">
            {grantTypeOptions.map((option) => (
              <label key={option.value} className="flex items-center gap-2 text-sm text-slate-200">
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-slate-600 bg-slate-950 text-indigo-500"
                  checked={form.grantTypes.includes(option.value)}
                  onChange={(event) => {
                    const nextGrantTypes = event.target.checked
                      ? [...form.grantTypes, option.value]
                      : form.grantTypes.filter((item) => item !== option.value);
                    setForm((prev) => ({
                      ...prev,
                      grantTypes: nextGrantTypes,
                    }));
                    setErrors((current) => ({
                      ...current,
                      grantTypes: validateGrantTypes(nextGrantTypes) ?? undefined,
                    }));
                  }}
                  disabled={presetDetail?.lockedFields.includes("grantTypes") || (form.clientType === "public" && option.value === "client_credentials")}
                />
                {option.label}
              </label>
            ))}
            {enablePasswordGrant && (
              <label className="col-span-full flex items-center gap-2 rounded-md border border-amber-500/40 bg-amber-500/10 p-2 text-sm text-amber-200">
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-amber-500/60 bg-slate-950 text-amber-400"
                  checked={form.grantTypes.includes("password")}
                  onChange={(event) => {
                    const nextGrantTypes = event.target.checked
                      ? [...form.grantTypes, "password"]
                      : form.grantTypes.filter((item) => item !== "password");
                    setForm((prev) => ({
                      ...prev,
                      grantTypes: nextGrantTypes,
                    }));
                    setErrors((current) => ({
                      ...current,
                      grantTypes: validateGrantTypes(nextGrantTypes) ?? undefined,
                    }));
                  }}
                  disabled={presetDetail?.lockedFields.includes("grantTypes") || form.clientType === "public"}
                />
                <span>Advanced / Legacy: Password grant <strong>(Legacy / not recommended)</strong></span>
              </label>
            )}
            {!enablePasswordGrant && (
              <p className="col-span-full text-xs text-slate-400">Password grant is disabled by server feature flag.</p>
            )}
          </div>
        </Field>
        {form.clientType === "public" && (
          <div data-lint-field="pkceRequired">
          <Field
            label="PKCE"
            tooltip="PKCE zvyšuje bezpečnost Authorization Code flow u public klientů. Pokud je zapnuto, musí být povolen authorization_code."
            hint="Doporučeno pro SPA/mobile klienty."
            error={errors.pkceRequired}
          >
            <label className="flex items-center gap-2 text-sm text-slate-200">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-slate-600 bg-slate-950 text-indigo-500"
                checked={form.pkceRequired}
                onChange={(event) => {
                  setForm((prev) => ({ ...prev, pkceRequired: event.target.checked }));
                  setErrors((current) => ({
                    ...current,
                    pkceRequired:
                      validatePkceRules(
                        form.clientType,
                        form.grantTypes,
                        event.target.checked
                      ) ?? undefined,
                  }));
                }}
                disabled={presetDetail?.lockedFields.includes("pkceRequired")}
              />
              Require PKCE
            </label>
          </Field>
          {renderInlineFindings("pkceRequired")}
          </div>
        )}

        <div className="mt-5 grid gap-4 md:grid-cols-2">
          <Field label="Allow introspection (RFC 7662)">
            <label className="flex items-center gap-2 text-sm text-slate-200">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-slate-600 bg-slate-950 text-indigo-500"
                checked={form.allowIntrospection}
                onChange={(event) => setForm((prev) => ({ ...prev, allowIntrospection: event.target.checked }))}
              />
              Allow /connect/introspect
            </label>
          </Field>

          <Field label="Allow user credentials (password grant)" tooltip="Legacy flow. Increases phishing/password exposure risk and is not recommended." error={errors.allowUserCredentials}>
            <label className="flex items-center gap-2 text-sm text-slate-200">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-slate-600 bg-slate-950 text-indigo-500"
                checked={form.allowUserCredentials}
                onChange={(event) => setForm((prev) => ({ ...prev, allowUserCredentials: event.target.checked }))}
                disabled={!enablePasswordGrant}
              />
              Restricted integration-only password grant
            </label>
          </Field>
          {renderInlineFindings("postLogoutRedirectUris")}
          </div>
        </div>
      </section>
      )}

      {(activeProfileRule?.sections.scopes ?? true) && (
      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
        <div data-lint-field="scopes">
        <Field
          label="Scopes"
          tooltip="Vyber scope, které může klient požadovat. Scopes jsou navázané na API resources."
          hint="Nejprve vytvoř scopes v OIDC Scopes."
          error={errors.scopes}
        >
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {scopes.map((scope) => (
              <label key={scope.name} className="flex items-center gap-2 text-sm text-slate-200">
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-slate-600 bg-slate-950 text-indigo-500"
                  checked={form.scopes.includes(scope.name)}
                  onChange={(event) =>
                    setForm((prev) => ({
                      ...prev,
                      scopes: event.target.checked
                        ? [...prev.scopes, scope.name]
                        : prev.scopes.filter((item) => item !== scope.name),
                    }))
                  }
                />
                <span className="font-medium">{scope.name}</span>
                {scope.displayName && (
                  <span className="text-xs text-slate-400">{scope.displayName}</span>
                )}
              </label>
            ))}
            {scopes.length === 0 && (
              <p className="text-sm text-slate-400">No scopes yet — create one.</p>
            )}
          </div>
        </Field>
        {renderInlineFindings("scopes")}
        </div>

        {form.allowUserCredentials && (
          <Field
            label="Allowed scopes for password grant"
            hint="Requested scopes for password grant must be a subset of this list."
            error={errors.allowedScopesForPasswordGrant}
          >
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {scopes.map((scope) => (
                <label key={`pwd-${scope.name}`} className="flex items-center gap-2 text-sm text-slate-200">
                  <input
                    type="checkbox"
                    className="h-4 w-4 rounded border-slate-600 bg-slate-950 text-indigo-500"
                    checked={form.allowedScopesForPasswordGrant.includes(scope.name)}
                    onChange={(event) =>
                      setForm((prev) => ({
                        ...prev,
                        allowedScopesForPasswordGrant: event.target.checked
                          ? [...prev.allowedScopesForPasswordGrant, scope.name]
                          : prev.allowedScopesForPasswordGrant.filter((item) => item !== scope.name),
                      }))
                    }
                  />
                  <span className="font-medium">{scope.name}</span>
                </label>
              ))}
            </div>
          </Field>
        )}
      </section>
      )}

      {(activeProfileRule?.sections.redirectUris ?? true) && (
      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
        <div className="grid gap-5 md:grid-cols-2">
          <div data-lint-field="redirectUris">
          <Field
            label="Redirect URIs"
            tooltip="URL, kam se vrací uživatel po přihlášení. Musí být absolutní, HTTPS (v dev povol http://localhost)."
            hint="1 URI per line. Povinné pro authorization_code."
            error={errors.redirectUris}
            required={form.grantTypes.includes("authorization_code")}
          >
            <MultiLineListInput
              value={form.redirectUrisText}
              onChange={(value) => setForm((prev) => ({ ...prev, redirectUrisText: value }))}
              onBlur={() => runValidation(form)}
              placeholder="http://localhost:5173/auth/callback"
              error={errors.redirectUris}
              lineErrors={redirectErrors}
              minRows={5}
            />
          </Field>
          {renderInlineFindings("redirectUris")}
          </div>

          <div data-lint-field="postLogoutRedirectUris">
          <Field
            label="Post logout redirect URIs"
            tooltip="URL, kam se vrací uživatel po odhlášení. Musí být absolutní a bez fragmentu."
            hint="1 URI per line. Volitelné."
            error={errors.postLogoutRedirectUris}
          >
            <MultiLineListInput
              value={form.postLogoutRedirectUrisText}
              onChange={(value) =>
                setForm((prev) => ({ ...prev, postLogoutRedirectUrisText: value }))
              }
              onBlur={() => runValidation(form)}
              placeholder="https://app.example.com/logout/callback"
              error={errors.postLogoutRedirectUris}
              lineErrors={postLogoutErrors}
              minRows={5}
            />
          </Field>
          {renderInlineFindings("postLogoutRedirectUris")}
          </div>
        </div>
      </section>
      )}

      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
        <h2 className="text-lg font-semibold text-white">Branding</h2>
        <p className="mt-1 text-sm text-slate-400">Optional application-specific details shown on login and registration pages.</p>
        <div className="mt-4 grid gap-5 md:grid-cols-2">
          <Field label="Display name override" hint="Optional label shown to users during sign-in.">
            <input className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100" value={form.brandingDisplayName} onChange={(event) => setForm((prev) => ({ ...prev, brandingDisplayName: event.target.value }))} />
          </Field>
          <Field label="Logo URL" hint="Absolute URL. HTTPS required by server in production." error={errors.brandingLogoUrl}>
            <input className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100" value={form.brandingLogoUrl} onChange={(event) => setForm((prev) => ({ ...prev, brandingLogoUrl: event.target.value }))} />
          </Field>
          <Field label="Home URL" error={errors.brandingHomeUrl}>
            <input className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100" value={form.brandingHomeUrl} onChange={(event) => setForm((prev) => ({ ...prev, brandingHomeUrl: event.target.value }))} />
          </Field>
          <Field label="Privacy URL" error={errors.brandingPrivacyUrl}>
            <input className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100" value={form.brandingPrivacyUrl} onChange={(event) => setForm((prev) => ({ ...prev, brandingPrivacyUrl: event.target.value }))} />
          </Field>
          <Field label="Terms URL" error={errors.brandingTermsUrl}>
            <input className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100" value={form.brandingTermsUrl} onChange={(event) => setForm((prev) => ({ ...prev, brandingTermsUrl: event.target.value }))} />
          </Field>
        </div>
      </section>

      {secretNotice && (
        <div className="rounded-lg border border-amber-500/40 bg-amber-500/10 p-4 text-sm text-amber-200">
          {secretNotice}
        </div>
      )}

      {showSecret && secret && (
        <div className="fixed inset-0 z-40 flex items-center justify-center bg-slate-950/80 px-4">
          <div className="w-full max-w-lg rounded-xl border border-slate-700 bg-slate-900 p-6 text-slate-100 shadow-xl">
            <h3 className="text-lg font-semibold text-white">Client secret</h3>
            <p className="mt-2 text-sm text-slate-300">
              This secret is shown only once. Copy it and store it securely.
            </p>
            <div className="mt-4 rounded-md border border-slate-700 bg-slate-950 px-3 py-2 font-mono text-sm">
              {secret}
            </div>
            <div className="mt-6 flex justify-end">
              <button
                type="button"
                className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400"
                onClick={() => setShowSecret(false)}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </form>
  );
}
