import { useState } from "react";
import { AppError, apiRequest } from "../api/http";
import type { AdminRevocationResult } from "../api/types";
import { Field, FormError } from "../components/Field";
import { pushToast } from "../components/toast";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

export default function SecurityRevocation() {
  const [userId, setUserId] = useState("");
  const [clientId, setClientId] = useState("");
  const [result, setResult] = useState<AdminRevocationResult | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [formDiagnostics, setFormDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  const handleRequest = async (endpoint: string) => {
    setFormError(null);
    setFormDiagnostics(undefined);
    setFieldErrors({});
    try {
      const response = await apiRequest<AdminRevocationResult>(endpoint, {
        method: "POST",
        suppressToast: true,
      });
      setResult(response);
      pushToast({ tone: "success", message: "Revocation executed." });
    } catch (error) {
      const parsed = parseProblemDetailsErrors(error);
      setFormError(parsed.generalError);
      setFormDiagnostics(parsed.diagnostics);
      setFieldErrors(parsed.fieldErrors ?? {});
      if (!(error instanceof AppError)) {
        pushToast({ tone: "error", message: "Failed to revoke grants." });
      }
    }
  };

  const handleUserRevoke = async () => {
    await handleRequest(`/admin/api/security/revoke/user/${userId}`);
  };

  const handleClientRevoke = async () => {
    await handleRequest(`/admin/api/security/revoke/client/${clientId}`);
  };

  const handleUserClientRevoke = async () => {
    await handleRequest(`/admin/api/security/revoke/user/${userId}/client/${clientId}`);
  };

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">Revocation</h1>
        <p className="mt-1 text-sm text-slate-400">
          Revoke tokens and authorizations by user, client, or both.
        </p>
      </div>

      <div className="rounded-lg border border-slate-800 bg-slate-950/60 p-5">
        <div className="grid gap-4 md:grid-cols-2">
          <Field label="User ID" error={fieldErrors?.["userId"]?.[0]}>
            <input
              type="text"
              value={userId}
              onChange={(event) => setUserId(event.target.value)}
              placeholder="GUID"
              className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
            />
          </Field>
          <Field label="Client ID" error={fieldErrors?.["clientId"]?.[0]}>
            <input
              type="text"
              value={clientId}
              onChange={(event) => setClientId(event.target.value)}
              placeholder="client-id"
              className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white"
            />
          </Field>
        </div>

        <div className="mt-4 flex flex-wrap gap-3">
          <button
            type="button"
            onClick={handleUserRevoke}
            disabled={!userId}
            className="rounded-md border border-slate-700 px-3 py-2 text-sm text-slate-200 hover:border-slate-500 disabled:opacity-50"
          >
            Revoke user grants
          </button>
          <button
            type="button"
            onClick={handleClientRevoke}
            disabled={!clientId}
            className="rounded-md border border-slate-700 px-3 py-2 text-sm text-slate-200 hover:border-slate-500 disabled:opacity-50"
          >
            Revoke client grants
          </button>
          <button
            type="button"
            onClick={handleUserClientRevoke}
            disabled={!userId || !clientId}
            className="rounded-md border border-rose-500/70 px-3 py-2 text-sm text-rose-200 hover:border-rose-400 disabled:opacity-50"
          >
            Revoke user + client
          </button>
        </div>

        {result && (
          <div className="mt-4 rounded-md border border-slate-800 bg-slate-950/70 p-4 text-sm text-slate-200">
            Revoked {result.tokensRevoked} tokens and {result.authorizationsRevoked} authorizations.
          </div>
        )}
      </div>

      <FormError message={formError} diagnostics={formDiagnostics} />
    </div>
  );
}
