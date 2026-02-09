import { useEffect, useState } from "react";
import { AppError, apiRequest } from "../api/http";
import type { AdminUserLifecyclePolicy, AdminUserLifecyclePreview } from "../api/types";
import { pushToast } from "../components/toast";

const emptyPolicy: AdminUserLifecyclePolicy = {
  id: "",
  enabled: true,
  deactivateAfterDays: 90,
  deleteAfterDays: 365,
  hardDeleteAfterDays: null,
  hardDeleteEnabled: false,
  warnBeforeLogoutMinutes: 5,
  idleLogoutMinutes: 30,
  updatedUtc: new Date().toISOString(),
};

export default function UserLifecyclePolicy() {
  const [policy, setPolicy] = useState<AdminUserLifecyclePolicy>(emptyPolicy);
  const [preview, setPreview] = useState<AdminUserLifecyclePreview | null>(null);
  const [days, setDays] = useState(90);
  const [targetUserId, setTargetUserId] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiRequest<AdminUserLifecyclePolicy>("/admin/api/security/user-lifecycle-policy")
      .then(setPolicy)
      .catch((err) => {
        if (err instanceof AppError && err.status === 403) {
          setError("Insufficient permissions to manage user lifecycle policy.");
          return;
        }

        throw err;
      });
  }, []);

  const save = async () => {
    const updated = await apiRequest<AdminUserLifecyclePolicy>("/admin/api/security/user-lifecycle-policy", {
      method: "PUT",
      body: JSON.stringify(policy),
    });
    setPolicy(updated);
    pushToast({ tone: "success", message: "User lifecycle policy updated." });
  };


  const runAction = async (action: "deactivate" | "anonymize" | "hard-delete") => {
    if (!targetUserId) return;
    const suffix = action === "hard-delete" ? "?confirm=true" : "";
    await apiRequest<void>(`/admin/api/security/users/${targetUserId}/${action}${suffix}`, { method: "POST" });
    pushToast({ tone: "success", message: `User ${action} executed.` });
  };

  const loadPreview = async () => {
    const next = await apiRequest<AdminUserLifecyclePreview>(`/admin/api/security/user-lifecycle/preview?days=${days}`);
    setPreview(next);
  };

  if (error) {
    return <div className="rounded-xl border border-rose-800 bg-rose-950/30 px-4 py-3 text-sm text-rose-200">{error}</div>;
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">User Lifecycle</h1>
        <p className="mt-1 text-sm text-slate-400">Manage inactivity deactivation, anonymization, and optional hard delete.</p>
      </div>
      <div className="grid gap-4 md:grid-cols-2">
        <label className="text-sm text-slate-300">Deactivate after days
          <input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-3 py-2" value={policy.deactivateAfterDays} onChange={(e) => setPolicy((p) => ({ ...p, deactivateAfterDays: Number(e.target.value) }))} />
        </label>
        <label className="text-sm text-slate-300">Anonymize after days
          <input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-3 py-2" value={policy.deleteAfterDays} onChange={(e) => setPolicy((p) => ({ ...p, deleteAfterDays: Number(e.target.value) }))} />
        </label>
        <label className="text-sm text-slate-300">Hard delete enabled
          <input type="checkbox" className="ml-3" checked={policy.hardDeleteEnabled} onChange={(e) => setPolicy((p) => ({ ...p, hardDeleteEnabled: e.target.checked }))} />
        </label>
        <label className="text-sm text-slate-300">Hard delete after days
          <input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-3 py-2" value={policy.hardDeleteAfterDays ?? 0} onChange={(e) => setPolicy((p) => ({ ...p, hardDeleteAfterDays: Number(e.target.value) }))} />
        </label>
      </div>
      <div className="flex gap-3">
        <button className="rounded bg-indigo-600 px-4 py-2 text-sm" onClick={save}>Save policy</button>
        <input type="number" className="w-28 rounded border border-slate-700 bg-slate-950 px-3 py-2 text-sm" value={days} onChange={(e) => setDays(Number(e.target.value))} />
        <button className="rounded border border-slate-700 px-4 py-2 text-sm" onClick={loadPreview}>Preview</button>
      </div>
      {preview ? <div className="rounded border border-slate-800 p-4 text-sm text-slate-300">Would deactivate: {preview.wouldDeactivate} · Would anonymize: {preview.wouldAnonymize} · Would hard delete: {preview.wouldHardDelete}</div> : null}
      <div className="rounded border border-slate-800 p-4">
        <h2 className="mb-3 text-sm font-semibold text-white">Manual user lifecycle actions</h2>
        <input className="mb-3 w-full rounded border border-slate-700 bg-slate-950 px-3 py-2 text-sm" placeholder="User ID (GUID)" value={targetUserId} onChange={(e) => setTargetUserId(e.target.value)} />
        <div className="flex gap-2">
          <button className="rounded border border-slate-700 px-3 py-2 text-sm" onClick={() => runAction("deactivate")}>Deactivate</button>
          <button className="rounded border border-slate-700 px-3 py-2 text-sm" onClick={() => runAction("anonymize")}>Anonymize</button>
          <button className="rounded border border-rose-500/50 px-3 py-2 text-sm text-rose-200" onClick={() => runAction("hard-delete")}>Hard Delete</button>
        </div>
      </div>
    </div>
  );
}
