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
  Revoked: "bg-rose-500/20 text-rose-200",
};

const formatDate = (value?: string | null) =>
  value ? new Date(value).toLocaleString() : "—";

type KeyAction = {
  type: "rotate" | "retire" | "revoke";
  key?: AdminSigningKeyRingResponse["keys"][number];
};

export default function SigningKeys() {
  const [data, setData] = useState<AdminSigningKeyRingResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [rotating, setRotating] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [pendingAction, setPendingAction] = useState<KeyAction | null>(null);

  const load = async () => {
    setLoading(true);
    const response = await apiRequest<AdminSigningKeyRingResponse>(
      "/admin/api/security/keys/signing"
    );
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
        "/admin/api/security/keys/signing/rotate",
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

  const retireKey = async (kid: string) => {
    setRotating(true);
    try {
      await apiRequest(`/admin/api/security/keys/signing/${kid}/retire`, { method: "POST" });
      pushToast({ tone: "success", message: "Signing key retired." });
      await load();
    } finally {
      setRotating(false);
      setPendingAction(null);
    }
  };

  const revokeKey = async (kid: string) => {
    setRotating(true);
    try {
      await apiRequest(`/admin/api/security/keys/signing/${kid}/revoke`, { method: "POST" });
      pushToast({ tone: "warning", message: "Signing key revoked." });
      await load();
    } finally {
      setRotating(false);
      setPendingAction(null);
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
                <th className="px-4 py-3 font-semibold">Retire after</th>
                <th className="px-4 py-3 font-semibold">Retired</th>
                <th className="px-4 py-3 font-semibold">Revoked</th>
                <th className="px-4 py-3 font-semibold">Algorithm</th>
                <th className="px-4 py-3 font-semibold">Published</th>
                <th className="px-4 py-3 font-semibold">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800 text-slate-200">
              {loading && (
                <tr>
                  <td className="px-4 py-4 text-sm text-slate-400" colSpan={10}>
                    Loading signing keys…
                  </td>
                </tr>
              )}
              {!loading && data?.keys.length === 0 && (
                <tr>
                  <td className="px-4 py-4 text-sm text-slate-400" colSpan={10}>
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
                  <td className="px-4 py-3">{formatDate(key.retireAfterUtc)}</td>
                  <td className="px-4 py-3">{formatDate(key.retiredUtc)}</td>
                  <td className="px-4 py-3">{formatDate(key.revokedUtc)}</td>
                  <td className="px-4 py-3">
                    {key.algorithm} · {key.keyType}
                  </td>
                  <td className="px-4 py-3">{key.isPublished ? "Yes" : "No"}</td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-2">
                      <button
                        type="button"
                        className="rounded-md border border-slate-700 px-2 py-1 text-xs text-slate-200 hover:border-slate-500 disabled:opacity-50"
                        disabled={rotating || key.status !== "Previous"}
                        onClick={() => setPendingAction({ type: "retire", key })}
                      >
                        Retire
                      </button>
                      <button
                        type="button"
                        className="rounded-md border border-rose-500/60 px-2 py-1 text-xs text-rose-200 hover:border-rose-400 disabled:opacity-50"
                        disabled={rotating || key.status === "Revoked" || key.status === "Retired"}
                        onClick={() => setPendingAction({ type: "revoke", key })}
                      >
                        Revoke
                      </button>
                    </div>
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
            <div className="text-xs uppercase tracking-wide text-slate-500">Last rotation</div>
            <div className="mt-2 text-sm text-slate-200">
              {data?.lastRotationUtc ? formatDate(data.lastRotationUtc) : "—"}
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
          <div className="rounded-lg border border-slate-800 bg-slate-950/80 p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">Next rotation check</div>
            <div className="mt-2 text-sm text-slate-200">
              {data?.nextRotationCheckUtc ? formatDate(data.nextRotationCheckUtc) : "—"}
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

      <ConfirmDialog
        title={
          pendingAction?.type === "revoke" ? "Revoke signing key?" : "Retire signing key?"
        }
        description={
          pendingAction?.type === "revoke"
            ? "Revoking a key immediately removes it from JWKS and invalidates tokens signed with it."
            : "Retiring a previous key stops publishing it in JWKS."
        }
        confirmLabel={pendingAction?.type === "revoke" ? "Revoke" : "Retire"}
        isOpen={!!pendingAction}
        confirmDisabled={rotating}
        onCancel={() => setPendingAction(null)}
        onConfirm={() => {
          if (!pendingAction?.key) {
            setPendingAction(null);
            return;
          }
          if (pendingAction.type === "revoke") {
            void revokeKey(pendingAction.key.kid);
            return;
          }
          void retireKey(pendingAction.key.kid);
        }}
      />
    </div>
  );
}
