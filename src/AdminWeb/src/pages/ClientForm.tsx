import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { apiRequest } from "../api/http";
import type {
  AdminClientDetail,
  AdminClientSecretResponse,
  AdminOidcScopeListItem,
  PagedResult,
} from "../api/types";
import { pushToast } from "../components/toast";

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

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
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
        redirectUris: splitLines(form.redirectUrisText),
        postLogoutRedirectUris: splitLines(form.postLogoutRedirectUrisText),
      };

      if (mode === "create") {
        const response = await apiRequest<AdminClientSecretResponse>("/admin/api/clients", {
          method: "POST",
          body: JSON.stringify(payload),
        });
        if (response.clientSecret) {
          setSecret(response.clientSecret);
          setShowSecret(true);
        }
        pushToast({ message: "Client created.", tone: "success" });
        navigate(`/oidc/clients/${response.client.id}`, { state: { secret: response.clientSecret } });
        return;
      }

      if (!clientId) {
        return;
      }

      await apiRequest<AdminClientDetail>(`/admin/api/clients/${clientId}`, {
        method: "PUT",
        body: JSON.stringify(payload),
      });
      pushToast({ message: "Client updated.", tone: "success" });
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
        <div className="grid gap-5 md:grid-cols-2">
          <label className="flex flex-col gap-2 text-sm text-slate-200">
            Client ID
            <input
              className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              value={form.clientId}
              onChange={(event) => setForm((prev) => ({ ...prev, clientId: event.target.value }))}
              required
            />
          </label>

          <label className="flex flex-col gap-2 text-sm text-slate-200">
            Display name
            <input
              className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              value={form.displayName}
              onChange={(event) => setForm((prev) => ({ ...prev, displayName: event.target.value }))}
            />
          </label>

          <label className="flex flex-col gap-2 text-sm text-slate-200">
            Client type
            <select
              className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              value={form.clientType}
              onChange={(event) =>
                setForm((prev) => ({
                  ...prev,
                  clientType: event.target.value as FormState["clientType"],
                }))
              }
            >
              <option value="public">Public</option>
              <option value="confidential">Confidential</option>
            </select>
          </label>

          <label className="flex items-center gap-3 text-sm text-slate-200">
            <input
              type="checkbox"
              className="h-4 w-4 rounded border-slate-600 bg-slate-950 text-indigo-500"
              checked={form.enabled}
              onChange={(event) => setForm((prev) => ({ ...prev, enabled: event.target.checked }))}
            />
            Enabled
          </label>
        </div>
      </section>

      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
        <h2 className="text-lg font-semibold text-white">Grant types</h2>
        <div className="mt-4 flex flex-wrap gap-4">
          {grantTypeOptions.map((option) => (
            <label key={option.value} className="flex items-center gap-2 text-sm text-slate-200">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-slate-600 bg-slate-950 text-indigo-500"
                checked={form.grantTypes.includes(option.value)}
                onChange={(event) => {
                  setForm((prev) => ({
                    ...prev,
                    grantTypes: event.target.checked
                      ? [...prev.grantTypes, option.value]
                      : prev.grantTypes.filter((item) => item !== option.value),
                  }));
                }}
                disabled={form.clientType === "public" && option.value === "client_credentials"}
              />
              {option.label}
            </label>
          ))}
        </div>
        {form.clientType === "public" && (
          <label className="mt-4 flex items-center gap-2 text-sm text-slate-200">
            <input
              type="checkbox"
              className="h-4 w-4 rounded border-slate-600 bg-slate-950 text-indigo-500"
              checked={form.pkceRequired}
              onChange={(event) =>
                setForm((prev) => ({ ...prev, pkceRequired: event.target.checked }))
              }
            />
            Require PKCE
          </label>
        )}
      </section>

      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
        <h2 className="text-lg font-semibold text-white">Scopes</h2>
        <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
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
              {scope.displayName && <span className="text-xs text-slate-400">{scope.displayName}</span>}
            </label>
          ))}
          {scopes.length === 0 && <p className="text-sm text-slate-400">No scopes available.</p>}
        </div>
      </section>

      <section className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
        <div className="grid gap-5 md:grid-cols-2">
          <label className="flex flex-col gap-2 text-sm text-slate-200">
            Redirect URIs (one per line)
            <textarea
              className="min-h-[120px] rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              value={form.redirectUrisText}
              onChange={(event) =>
                setForm((prev) => ({ ...prev, redirectUrisText: event.target.value }))
              }
            />
          </label>

          <label className="flex flex-col gap-2 text-sm text-slate-200">
            Post logout redirect URIs (one per line)
            <textarea
              className="min-h-[120px] rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              value={form.postLogoutRedirectUrisText}
              onChange={(event) =>
                setForm((prev) => ({
                  ...prev,
                  postLogoutRedirectUrisText: event.target.value,
                }))
              }
            />
          </label>
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

function splitLines(value: string) {
  return value
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);
}
