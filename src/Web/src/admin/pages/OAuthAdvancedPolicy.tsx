import { useEffect, useMemo, useState } from "react";
import { AppError, apiRequest } from "../api/http";
import type { AdminOAuthAdvancedPolicy, AdminOAuthAdvancedPolicyResponse, UpdateAdminOAuthAdvancedPolicyRequest } from "../api/types";
import ConfirmDialog from "../components/ConfirmDialog";
import { FormError } from "../components/Field";
import { pushToast } from "../components/toast";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

const defaults: AdminOAuthAdvancedPolicy = {
  deviceFlowEnabled: false,
  deviceFlowUserCodeTtlMinutes: 10,
  deviceFlowPollingIntervalSeconds: 5,
  tokenExchangeEnabled: false,
  tokenExchangeAllowedClientIds: [],
  tokenExchangeAllowedAudiences: [],
  refreshRotationEnabled: false,
  refreshReuseDetectionEnabled: false,
  refreshReuseAction: "RevokeFamily",
  backChannelLogoutEnabled: false,
  frontChannelLogoutEnabled: false,
  logoutTokenTtlMinutes: 5,
  updatedAtUtc: new Date(0).toISOString(),
  rowVersion: "",
};

export default function OAuthAdvancedPolicy() {
  const [policy, setPolicy] = useState<AdminOAuthAdvancedPolicy>(defaults);
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);
  const [formError, setFormError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [confirmRisky, setConfirmRisky] = useState(false);

  useEffect(() => {
    apiRequest<AdminOAuthAdvancedPolicyResponse>("/admin/api/security/oauth-advanced-policy")
      .then((response) => setPolicy(response.policy))
      .finally(() => setLoading(false));
  }, []);

  const riskyEnabled = useMemo(() => policy.tokenExchangeEnabled || policy.frontChannelLogoutEnabled, [policy]);

  const save = async (breakGlass = false) => {
    setSaving(true);
    setFormError(null);
    setFieldErrors({});
    const payload: UpdateAdminOAuthAdvancedPolicyRequest = {
      ...policy,
      breakGlass,
    };

    try {
      const response = await apiRequest<AdminOAuthAdvancedPolicyResponse>("/admin/api/security/oauth-advanced-policy", {
        method: "PUT",
        body: JSON.stringify(payload),
        suppressToast: true,
      });
      setPolicy(response.policy);
      pushToast({ tone: "success", message: "OAuth advanced policy updated." });
    } catch (error) {
      const parsed = parseProblemDetailsErrors(error);
      setFormError(parsed.generalError);
      setFieldErrors(parsed.fieldErrors ?? {});
      if (!(error instanceof AppError)) {
        pushToast({ tone: "error", message: "Failed to save OAuth advanced policy." });
      }
    } finally {
      setSaving(false);
      setConfirmRisky(false);
    }
  };

  if (loading) return <div className="text-sm text-slate-400">Loading OAuth advanced policy…</div>;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">OAuth Advanced Features</h1>
        <p className="mt-1 text-sm text-slate-400">Govern risky OAuth/IAM features with runtime policy controls and production guardrails.</p>
      </div>

      {riskyEnabled && <div className="rounded-lg border border-amber-500/40 bg-amber-500/10 p-3 text-sm text-amber-200">High-risk features are enabled. Break-glass is required in production.</div>}

      <div className="grid gap-4 rounded-lg border border-slate-800 bg-slate-950/60 p-5 md:grid-cols-2">
        <label className="text-sm text-slate-200"><input type="checkbox" checked={policy.deviceFlowEnabled} onChange={(e)=>setPolicy({...policy, deviceFlowEnabled:e.target.checked})} /> Device Code Flow</label>
        <label className="text-sm text-slate-200"><input type="checkbox" checked={policy.tokenExchangeEnabled} onChange={(e)=>setPolicy({...policy, tokenExchangeEnabled:e.target.checked})} /> Token Exchange</label>
        <label className="text-sm text-slate-200"><input type="checkbox" checked={policy.refreshRotationEnabled} onChange={(e)=>setPolicy({...policy, refreshRotationEnabled:e.target.checked})} /> Refresh rotation</label>
        <label className="text-sm text-slate-200"><input type="checkbox" checked={policy.refreshReuseDetectionEnabled} onChange={(e)=>setPolicy({...policy, refreshReuseDetectionEnabled:e.target.checked})} /> Refresh reuse detection</label>
        <label className="text-sm text-slate-200"><input type="checkbox" checked={policy.backChannelLogoutEnabled} onChange={(e)=>setPolicy({...policy, backChannelLogoutEnabled:e.target.checked})} /> Back-channel logout</label>
        <label className="text-sm text-slate-200"><input type="checkbox" checked={policy.frontChannelLogoutEnabled} onChange={(e)=>setPolicy({...policy, frontChannelLogoutEnabled:e.target.checked})} /> Front-channel logout</label>
        <label className="text-sm text-slate-300">Device code TTL (min)<input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-900 px-2 py-1" value={policy.deviceFlowUserCodeTtlMinutes} onChange={(e)=>setPolicy({...policy, deviceFlowUserCodeTtlMinutes:Number(e.target.value)})} /></label>
        <label className="text-sm text-slate-300">Device polling interval (s)<input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-900 px-2 py-1" value={policy.deviceFlowPollingIntervalSeconds} onChange={(e)=>setPolicy({...policy, deviceFlowPollingIntervalSeconds:Number(e.target.value)})} /></label>
        <label className="text-sm text-slate-300">Logout token TTL (min)<input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-900 px-2 py-1" value={policy.logoutTokenTtlMinutes} onChange={(e)=>setPolicy({...policy, logoutTokenTtlMinutes:Number(e.target.value)})} /></label>
        <label className="text-sm text-slate-300">Reuse action<select className="mt-1 w-full rounded border border-slate-700 bg-slate-900 px-2 py-1" value={policy.refreshReuseAction} onChange={(e)=>setPolicy({...policy, refreshReuseAction:e.target.value as AdminOAuthAdvancedPolicy["refreshReuseAction"]})}><option>RevokeFamily</option><option>RevokeAllSessions</option><option>IncidentOnly</option></select></label>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <label className="text-sm text-slate-300">Token exchange allowed client IDs (one per line)<textarea className="mt-1 min-h-28 w-full rounded border border-slate-700 bg-slate-900 px-2 py-1" value={policy.tokenExchangeAllowedClientIds.join("\n")} onChange={(e)=>setPolicy({...policy, tokenExchangeAllowedClientIds:e.target.value.split("\n").map(v=>v.trim()).filter(Boolean)})} /></label>
        <label className="text-sm text-slate-300">Token exchange allowed audiences (one per line)<textarea className="mt-1 min-h-28 w-full rounded border border-slate-700 bg-slate-900 px-2 py-1" value={policy.tokenExchangeAllowedAudiences.join("\n")} onChange={(e)=>setPolicy({...policy, tokenExchangeAllowedAudiences:e.target.value.split("\n").map(v=>v.trim()).filter(Boolean)})} /></label>
      </div>

      <p className="text-xs text-slate-400">Last updated: {new Date(policy.updatedAtUtc).toLocaleString()} {policy.updatedByUserId ? `by ${policy.updatedByUserId}` : ""}</p>
      <FormError message={formError ?? Object.values(fieldErrors)[0]?.[0]} />
      <div className="flex justify-end"><button className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white" disabled={saving} onClick={()=> riskyEnabled ? setConfirmRisky(true) : save(false)}>Save policy</button></div>

      <ConfirmDialog open={confirmRisky} title="Confirm high-risk changes" description="You are enabling token exchange or front-channel logout. Continue with break-glass override?" confirmLabel="Save with break-glass" onCancel={()=>setConfirmRisky(false)} onConfirm={()=>save(true)} />
    </div>
  );
}
