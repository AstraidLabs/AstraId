import { useEffect, useMemo, useState } from "react";
import { AppError, apiRequest } from "../api/http";
import type { AdminDeletionRequest, AdminPrivacyPolicy } from "../api/types";
import { GDPR_PERMISSIONS, hasAnyPermission } from "../../auth/adminAccess";
import { useAuthSession } from "../../auth/useAuthSession";

export default function SecurityPrivacy() {
  const { session } = useAuthSession();
  const [policy, setPolicy] = useState<AdminPrivacyPolicy | null>(null);
  const [requests, setRequests] = useState<AdminDeletionRequest[]>([]);
  const [error, setError] = useState<string | null>(null);

  const canRead = useMemo(
    () => hasAnyPermission(["gdpr.read"], session?.permissions),
    [session?.permissions]
  );
  const canManageRetention = useMemo(
    () => hasAnyPermission(["gdpr.retention.manage"], session?.permissions),
    [session?.permissions]
  );
  const canErase = useMemo(
    () => hasAnyPermission(["gdpr.erase"], session?.permissions),
    [session?.permissions]
  );

  const load = async () => {
    setError(null);

    try {
      const [p, r] = await Promise.all([
        apiRequest<AdminPrivacyPolicy>("/admin/api/security/privacy-policy"),
        apiRequest<AdminDeletionRequest[]>("/admin/api/security/deletion-requests")
      ]);
      setPolicy(p);
      setRequests(r);
    } catch (err) {
      if (err instanceof AppError && err.status === 403) {
        setError("You don’t have permission to view GDPR administration data.");
        setPolicy(null);
        setRequests([]);
        return;
      }

      setError("Unable to load GDPR administration data.");
      throw err;
    }
  };

  useEffect(() => {
    if (!hasAnyPermission(GDPR_PERMISSIONS, session?.permissions)) {
      setError("You don’t have permission to view GDPR administration data.");
      return;
    }

    if (!canRead) {
      setError("Missing gdpr.read permission.");
      return;
    }

    void load();
  }, [canRead, session?.permissions]);

  const savePolicy = async () => {
    if (!policy || !canManageRetention) return;
    const updated = await apiRequest<AdminPrivacyPolicy>("/admin/api/security/privacy-policy", { method: "PUT", body: JSON.stringify(policy) });
    setPolicy(updated);
  };

  const action = async (id: string, actionName: "approve" | "execute" | "cancel") => {
    if (!canErase) return;
    await apiRequest(`/admin/api/security/deletion-requests/${id}/${actionName}`, { method: "POST" });
    await load();
  };

  if (error) {
    return <div className="rounded-xl border border-rose-800 bg-rose-950/30 px-4 py-3 text-sm text-rose-200">{error}</div>;
  }

  return (
    <div className="space-y-6">
      <section className="rounded-xl border border-slate-800 p-4">
        <h2 className="text-lg font-semibold text-white">Privacy policy</h2>
        {policy ? <div className="mt-4 grid gap-3 md:grid-cols-3">
          <label className="text-sm text-slate-300">Login history days<input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-900 px-2 py-1" value={policy.loginHistoryRetentionDays} onChange={(e) => setPolicy({ ...policy, loginHistoryRetentionDays: Number(e.target.value) })} disabled={!canManageRetention} /></label>
          <label className="text-sm text-slate-300">Error log days<input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-900 px-2 py-1" value={policy.errorLogRetentionDays} onChange={(e) => setPolicy({ ...policy, errorLogRetentionDays: Number(e.target.value) })} disabled={!canManageRetention} /></label>
          <label className="text-sm text-slate-300">Audit log days<input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-900 px-2 py-1" value={policy.auditLogRetentionDays} onChange={(e) => setPolicy({ ...policy, auditLogRetentionDays: Number(e.target.value) })} disabled={!canManageRetention} /></label>
          <label className="text-sm text-slate-300">Deletion cooldown days<input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-900 px-2 py-1" value={policy.deletionCooldownDays} onChange={(e) => setPolicy({ ...policy, deletionCooldownDays: Number(e.target.value) })} disabled={!canManageRetention} /></label>
        </div> : null}
        <button onClick={() => void savePolicy()} className="mt-4 rounded bg-indigo-600 px-3 py-2 text-sm text-white disabled:cursor-not-allowed disabled:opacity-40" disabled={!canManageRetention}>Save policy</button>
      </section>

      <section className="rounded-xl border border-slate-800 p-4">
        <h2 className="text-lg font-semibold text-white">Deletion requests</h2>
        <table className="mt-3 min-w-full text-sm">
          <thead><tr className="text-slate-400"><th className="px-2 py-1 text-left">User</th><th className="px-2 py-1 text-left">Status</th><th className="px-2 py-1 text-left">Requested</th><th className="px-2 py-1 text-left">Actions</th></tr></thead>
          <tbody>
            {requests.map((r) => <tr key={r.id} className="border-t border-slate-800"><td className="px-2 py-1">{r.email ?? r.userName}</td><td className="px-2 py-1">{r.status}</td><td className="px-2 py-1">{new Date(r.requestedUtc).toLocaleString()}</td><td className="px-2 py-1 space-x-2"><button className="text-emerald-300 disabled:cursor-not-allowed disabled:opacity-40" disabled={!canErase} onClick={() => void action(r.id, "approve")}>Approve</button><button className="text-amber-300 disabled:cursor-not-allowed disabled:opacity-40" disabled={!canErase} onClick={() => void action(r.id, "execute")}>Execute</button><button className="text-rose-300 disabled:cursor-not-allowed disabled:opacity-40" disabled={!canErase} onClick={() => void action(r.id, "cancel")}>Cancel</button></td></tr>)}
          </tbody>
        </table>
      </section>
    </div>
  );
}
