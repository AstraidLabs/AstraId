import { useEffect, useState } from "react";
import { AppError, apiRequest } from "../api/http";
import type { AdminInactivityPolicy } from "../api/types";
import { pushToast } from "../components/toast";

const defaults: AdminInactivityPolicy = {
  id: "",
  enabled: true,
  warningAfterDays: 60,
  deactivateAfterDays: 90,
  deleteAfterDays: 365,
  warningRepeatDays: 14,
  deleteMode: 0,
  protectAdmins: true,
  protectedRoles: "Admin",
  updatedUtc: new Date().toISOString(),
};

export default function InactivityPolicy() {
  const [policy, setPolicy] = useState<AdminInactivityPolicy>(defaults);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiRequest<AdminInactivityPolicy>("/admin/api/security/inactivity-policy")
      .then(setPolicy)
      .catch((err) => {
        if (err instanceof AppError && err.status === 403) {
          setError("Insufficient permissions to manage inactivity policy.");
          return;
        }

        throw err;
      });
  }, []);

  const save = async () => {
    const updated = await apiRequest<AdminInactivityPolicy>("/admin/api/security/inactivity-policy", {
      method: "PUT",
      body: JSON.stringify(policy),
    });
    setPolicy(updated);
    pushToast({ tone: "success", message: "Inactivity policy updated." });
  };

  if (error) {
    return <div className="rounded-xl border border-rose-800 bg-rose-950/30 px-4 py-3 text-sm text-rose-200">{error}</div>;
  }

  return <div className="flex flex-col gap-4"><h1 className="text-2xl font-semibold text-white">Inactivity Policy</h1>
    <label className="text-sm">Warning after days<input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-3 py-2" value={policy.warningAfterDays} onChange={(e) => setPolicy((p) => ({ ...p, warningAfterDays: Number(e.target.value) }))} /></label>
    <label className="text-sm">Deactivate after days<input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-3 py-2" value={policy.deactivateAfterDays} onChange={(e) => setPolicy((p) => ({ ...p, deactivateAfterDays: Number(e.target.value) }))} /></label>
    <label className="text-sm">Delete after days<input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-3 py-2" value={policy.deleteAfterDays} onChange={(e) => setPolicy((p) => ({ ...p, deleteAfterDays: Number(e.target.value) }))} /></label>
    <label className="text-sm">Warning repeat days<input type="number" className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-3 py-2" value={policy.warningRepeatDays ?? 0} onChange={(e) => setPolicy((p) => ({ ...p, warningRepeatDays: Number(e.target.value) }))} /></label>
    <label className="text-sm">Protected roles (comma separated)<input type="text" className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-3 py-2" value={policy.protectedRoles} onChange={(e) => setPolicy((p) => ({ ...p, protectedRoles: e.target.value }))} /></label>
    <label className="text-sm">Delete mode<select className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-3 py-2" value={policy.deleteMode} onChange={(e) => setPolicy((p) => ({ ...p, deleteMode: Number(e.target.value) }))}><option value={0}>Anonymize</option><option value={1}>HardDelete</option></select></label>
    <button className="rounded bg-indigo-600 px-4 py-2 text-sm" onClick={save}>Save</button>
  </div>;
}
