import { useEffect, useState } from "react";
import { apiRequest } from "../api/http";
import type { AdminDataProtectionStatus, AdminEncryptionKeyStatus } from "../api/types";

const formatDate = (value?: string | null) => (value ? new Date(value).toLocaleString() : "—");

export default function SecurityDataProtection() {
  const [status, setStatus] = useState<AdminDataProtectionStatus | null>(null);
  const [encryption, setEncryption] = useState<AdminEncryptionKeyStatus | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const response = await apiRequest<AdminDataProtectionStatus>(
          "/admin/api/security/dataprotection/status"
        );
        setStatus(response);
        const encryptionResponse = await apiRequest<AdminEncryptionKeyStatus>(
          "/admin/api/security/encryption/status"
        );
        setEncryption(encryptionResponse);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  if (loading) {
    return <div className="text-sm text-slate-400">Loading DataProtection status…</div>;
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">Data Protection</h1>
        <p className="mt-1 text-sm text-slate-400">
          Monitor ASP.NET Core DataProtection key storage and read-only mode.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-4">
          <div className="text-xs uppercase tracking-wide text-slate-500">Persisted</div>
          <div className="mt-2 text-sm text-slate-200">
            {status?.keysPersisted ? "Yes" : "No"}
          </div>
        </div>
        <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-4">
          <div className="text-xs uppercase tracking-wide text-slate-500">Read-only</div>
          <div className="mt-2 text-sm text-slate-200">
            {status?.readOnly ? "Yes" : "No"}
          </div>
        </div>
        <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-4">
          <div className="text-xs uppercase tracking-wide text-slate-500">Key count</div>
          <div className="mt-2 text-sm text-slate-200">{status?.keyCount ?? "—"}</div>
        </div>
        <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-4 md:col-span-2">
          <div className="text-xs uppercase tracking-wide text-slate-500">Key path</div>
          <div className="mt-2 text-sm text-slate-200">
            {status?.keyPath || "Default platform storage"}
          </div>
        </div>
        <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-4">
          <div className="text-xs uppercase tracking-wide text-slate-500">Latest activation</div>
          <div className="mt-2 text-sm text-slate-200">
            {formatDate(status?.latestKeyActivationUtc)}
          </div>
        </div>
        <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-4">
          <div className="text-xs uppercase tracking-wide text-slate-500">Latest expiration</div>
          <div className="mt-2 text-sm text-slate-200">
            {formatDate(status?.latestKeyExpirationUtc)}
          </div>
        </div>
      </div>

      <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-5">
        <h2 className="text-lg font-semibold text-white">Encryption key status</h2>
        <p className="mt-1 text-sm text-slate-400">
          OpenIddict encryption certificate metadata (no private material shown).
        </p>
        <div className="mt-4 grid gap-4 md:grid-cols-3">
          <div className="rounded-lg border border-slate-800 bg-slate-950/80 p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">Enabled</div>
            <div className="mt-2 text-sm text-slate-200">
              {encryption?.enabled ? "Yes" : "No"}
            </div>
          </div>
          <div className="rounded-lg border border-slate-800 bg-slate-950/80 p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">Source</div>
            <div className="mt-2 text-sm text-slate-200">{encryption?.source ?? "—"}</div>
          </div>
          <div className="rounded-lg border border-slate-800 bg-slate-950/80 p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">Thumbprint</div>
            <div className="mt-2 break-all text-sm text-slate-200">
              {encryption?.thumbprint ?? "—"}
            </div>
          </div>
          <div className="rounded-lg border border-slate-800 bg-slate-950/80 p-4 md:col-span-2">
            <div className="text-xs uppercase tracking-wide text-slate-500">Subject</div>
            <div className="mt-2 text-sm text-slate-200">{encryption?.subject ?? "—"}</div>
          </div>
          <div className="rounded-lg border border-slate-800 bg-slate-950/80 p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">Not before</div>
            <div className="mt-2 text-sm text-slate-200">
              {formatDate(encryption?.notBeforeUtc)}
            </div>
          </div>
          <div className="rounded-lg border border-slate-800 bg-slate-950/80 p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">Not after</div>
            <div className="mt-2 text-sm text-slate-200">
              {formatDate(encryption?.notAfterUtc)}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
