import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { AppError, apiRequest } from "../api/http";
import type {
  AdminClientDetail,
  AdminClientSecretResponse,
  AdminOidcScopeListItem,
  PagedResult,
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
  scopes: string[];
  redirectUrisText: string;
  postLogoutRedirectUrisText: string;
};

type FormErrors = {
  clientId?: string;
  grantTypes?: string;
  pkceRequired?: string;
  redirectUris?: string;
  postLogoutRedirectUris?: string;
  scopes?: string;
};

const defaultState: FormState = {
  clientId: "",
  displayName: "",
  clientType: "public",
  enabled: true,
  grantTypes: ["authorization_code", "refresh_token"],
  pkceRequired: true,
  scopes: [],
  redirectUrisText: "",
  postLogoutRedirectUrisText: "",
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

  const isConfidential = form.clientType === "confidential";

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
  }, []);

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
          scopes: data.scopes,
          redirectUrisText: data.redirectUris.join("\n"),
          postLogoutRedirectUrisText: data.postLogoutRedirectUris.join("\n"),
        });
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
      const payload = {
        clientId: form.clientId.trim(),
        displayName: form.displayName.trim() || null,
        clientType: form.clientType,
        enabled: form.enabled,
        grantTypes: form.grantTypes,
        pkceRequired: form.clientType === "public" ? form.pkceRequired : false,
        scopes: form.scopes,
        redirectUris: parseRedirectUris(form.redirectUrisText),
        postLogoutRedirectUris: parseRedirectUris(form.postLogoutRedirectUrisText),
      };

      if (mode === "create") {
        const response = await apiRequest<AdminClientSecretResponse>("/admin/api/clients", {
          method: "POST",
          body: JSON.stringify(payload),
          suppressToast: true,
        });
        if (response.clientSecret) {
          setSecret(response.clientSecret);
          setShowSecret(true);
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

      await apiRequest<AdminClientDetail>(`/admin/api/clients/${clientId}`, {
        method: "PUT",
        body: JSON.stringify(payload),
        suppressToast: true,
      });
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
            disabled={saving}
            className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60"
          >
            {saving ? "Saving..." : "Save"}
          </button>
        </div>
      </div>

      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
      <FormError message={formError} diagnostics={formDiagnostics} />
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
        </div>
      </section>

      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
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
                  disabled={form.clientType === "public" && option.value === "client_credentials"}
                />
                {option.label}
              </label>
            ))}
          </div>
        </Field>
        {form.clientType === "public" && (
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
              />
              Require PKCE
            </label>
          </Field>
        )}
      </section>

      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
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
      </section>

      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
        <div className="grid gap-5 md:grid-cols-2">
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
        </div>
      </section>

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
