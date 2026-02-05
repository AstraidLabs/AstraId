import { useEffect, useState } from "react";
import { apiRequest } from "../api/http";
import type {
  AdminSigningKeyRingResponse,
  AdminSigningKeyRotationResponse,
} from "../api/types";
import ConfirmDialog from "../components/ConfirmDialog";
import { pushToast } from "../components/toast";

const statusTone: Record<string, string> = {
  Active: "bg-emerald-500/20 text-emerald-200",
  Previous: "bg-indigo-500/20 text-indigo-200",
  Retired: "bg-slate-700/40 text-slate-200",
};

const formatDate = (value?: string | null) =>
  value ? new Date(value).toLocaleString() : "—";

export default function SigningKeys() {
  const [data, setData] = useState<AdminSigningKeyRingResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [rotating, setRotating] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);

  const load = async () => {
    setLoading(true);
    const response = await apiRequest<AdminSigningKeyRingResponse>("/admin/api/signing-keys");
    setData(response);
    setLoading(false);
  };

  useEffect(() => {
    load();
  }, []);

  const rotateNow = async () => {
    setRotating(true);
    try {
      const response = await apiRequest<AdminSigningKeyRotationResponse>(
        "/admin/api/signing-keys/rotate",
        { method: "POST" }
      );
      pushToast({
        tone: "success",
        message: `Signing key rotated. Active kid: ${response.newActiveKid}.`,
      });
      await load();
    } finally {
      setRotating(false);
      setConfirmOpen(false);
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-start justify-between gap-6">
        <div>
          <h1 className="text-2xl font-semibold text-white">Signing Keys</h1>
          <p className="mt-1 text-sm text-slate-400">
            Manage the signing key ring used for token issuance and JWKS publication.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setConfirmOpen(true)}
          className="rounded-md bg-rose-500 px-4 py-2 text-sm font-semibold text-white hover:bg-rose-400 disabled:opacity-60"
          disabled={loading || rotating}
        >
          Rotate now
        </button>
      </div>

      <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-5">
        <h2 className="text-lg font-semibold text-white">Current key ring</h2>
        <p className="mt-1 text-sm text-slate-400">
          Active keys sign new tokens. Previous keys remain published for validation.
        </p>
        <div className="mt-4 overflow-hidden rounded-lg border border-slate-800">
          <table className="min-w-full divide-y divide-slate-800 text-sm">
            <thead className="bg-slate-950">
              <tr className="text-left text-xs uppercase tracking-wide text-slate-500">
                <th className="px-4 py-3 font-semibold">Status</th>
                <th className="px-4 py-3 font-semibold">Key ID</th>
                <th className="px-4 py-3 font-semibold">Created</th>
                <th className="px-4 py-3 font-semibold">Activated</th>
                <th className="px-4 py-3 font-semibold">Retired</th>
                <th className="px-4 py-3 font-semibold">Algorithm</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800 text-slate-200">
              {loading && (
                <tr>
                  <td className="px-4 py-4 text-sm text-slate-400" colSpan={6}>
                    Loading signing keys…
                  </td>
                </tr>
              )}
              {!loading && data?.keys.length === 0 && (
                <tr>
                  <td className="px-4 py-4 text-sm text-slate-400" colSpan={6}>
                    No signing keys found.
                  </td>
                </tr>
              )}
              {data?.keys.map((key) => (
                <tr key={key.kid}>
                  <td className="px-4 py-3">
                    <span
                      className={`rounded-full px-2 py-1 text-xs font-semibold ${
                        statusTone[key.status] ?? "bg-slate-700/40 text-slate-200"
                      }`}
                    >
                      {key.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 font-mono text-xs text-slate-300">{key.kid}</td>
                  <td className="px-4 py-3">{formatDate(key.createdUtc)}</td>
                  <td className="px-4 py-3">{formatDate(key.activatedUtc)}</td>
                  <td className="px-4 py-3">{formatDate(key.retiredUtc)}</td>
                  <td className="px-4 py-3">
                    {key.algorithm} · {key.keyType}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-5">
        <h2 className="text-lg font-semibold text-white">Auto-rotation status</h2>
        <div className="mt-4 grid gap-4 md:grid-cols-3">
          <div className="rounded-lg border border-slate-800 bg-slate-950/80 p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">Enabled</div>
            <div className="mt-2 text-sm text-slate-200">
              {data ? (data.rotationEnabled ? "Yes" : "No") : "—"}
            </div>
          </div>
          <div className="rounded-lg border border-slate-800 bg-slate-950/80 p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">Next rotation due</div>
            <div className="mt-2 text-sm text-slate-200">
              {data?.nextRotationDueUtc ? formatDate(data.nextRotationDueUtc) : "Not scheduled"}
            </div>
          </div>
          <div className="rounded-lg border border-slate-800 bg-slate-950/80 p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">Retention</div>
            <div className="mt-2 text-sm text-slate-200">
              {data ? `${data.retentionDays} days` : "—"}
            </div>
          </div>
          <div className="rounded-lg border border-slate-800 bg-slate-950/80 p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">Rotation interval</div>
            <div className="mt-2 text-sm text-slate-200">
              {data ? `${data.rotationIntervalDays} days` : "—"}
            </div>
          </div>
          <div className="rounded-lg border border-slate-800 bg-slate-950/80 p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">Check period</div>
            <div className="mt-2 text-sm text-slate-200">
              {data ? `${data.checkPeriodMinutes} minutes` : "—"}
            </div>
          </div>
          <div className="rounded-lg border border-slate-800 bg-slate-950/80 p-4 md:col-span-3">
            <div className="text-xs uppercase tracking-wide text-slate-500">JWKS exposure</div>
            <div className="mt-2 text-sm text-slate-200">
              Active + previous keys are published for validation.
            </div>
          </div>
        </div>
      </div>

      <ConfirmDialog
        title="Rotate signing keys?"
        description="A new active key will be generated and the previous key will remain published until it ages out. Existing tokens stay valid until expiry."
        confirmLabel="Rotate"
        isOpen={confirmOpen}
        confirmDisabled={rotating}
        onCancel={() => setConfirmOpen(false)}
        onConfirm={rotateNow}
      />
    </div>
  );
}
