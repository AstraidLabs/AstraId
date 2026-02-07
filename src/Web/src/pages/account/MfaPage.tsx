import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { confirmMfaSetupAccount, disableMfaAccount, getMfaStatusAccount, regenerateRecoveryCodesAccount, startMfaSetupAccount } from "../../account/api";
import type { MfaSetupResponse, MfaStatus } from "../../api/authServer";
import type { ParsedProblemResult } from "../../api/problemDetails";
import { mapErrorToProblem } from "../../account/errors";
import FormField from "../../components/account/FormField";
import InlineAlert from "../../components/account/InlineAlert";
import LoadingState from "../../components/account/LoadingState";
import PageHeader from "../../components/account/PageHeader";

export default function MfaPage() {
  const [status, setStatus] = useState<MfaStatus | null>(null);
  const [setup, setSetup] = useState<MfaSetupResponse | null>(null);
  const [setupCode, setSetupCode] = useState("");
  const [disableCode, setDisableCode] = useState("");
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [problem, setProblem] = useState<ParsedProblemResult | null>(null);
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState(false);
  const navigate = useNavigate();

  const load = async () => {
    setLoading(true);
    try {
      const data = await getMfaStatusAccount();
      setStatus(data);
    } catch (error) {
      const parsed = mapErrorToProblem(error, "Unable to load MFA status.");
      if (parsed.status === 401 || parsed.status === 403) {
        navigate(`/login?returnUrl=${encodeURIComponent("/account/mfa")}`, { replace: true });
        return;
      }
      setProblem(parsed);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const runAction = async (action: () => Promise<void>) => {
    setWorking(true);
    setProblem(null);
    setSuccess("");
    try {
      await action();
      await load();
    } catch (error) {
      setProblem(mapErrorToProblem(error, "Unable to complete MFA action."));
    } finally {
      setWorking(false);
    }
  };

  const codesText = useMemo(() => recoveryCodes.join("\n"), [recoveryCodes]);

  return (
    <div>
      <PageHeader title="MFA" description="Manage multi-factor authentication for your account." />
      {loading ? <LoadingState message="Loading MFA status..." /> : null}
      {success ? <InlineAlert kind="success" message={success} /> : null}
      {problem?.kind === "problem" ? <InlineAlert kind="error" message={`${problem.detail ?? "Request failed."}${problem.errorId ? ` (Error ID: ${problem.errorId})` : ""}`} /> : null}
      <div className="mt-4 space-y-4">
        <div className="rounded-lg border border-slate-800 bg-slate-950/50 p-4 text-sm text-slate-200">
          <p>MFA status: <span className="font-semibold text-white">{status?.enabled ? "Enabled" : "Disabled"}</span></p>
          <p className="mt-1">Recovery codes left: {status?.recoveryCodesLeft ?? 0}</p>
        </div>

        {!status?.enabled ? (
          <div className="space-y-3">
            <button type="button" className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60" disabled={working} onClick={() => runAction(async () => {
              const response = await startMfaSetupAccount();
              setSetup(response);
            })}>Start MFA setup</button>
            {setup ? (
              <div className="rounded-lg border border-slate-800 bg-slate-950/50 p-4">
                <p className="text-sm text-slate-300">Shared key</p>
                <code className="mt-2 block rounded bg-slate-900 px-2 py-2 text-xs text-slate-100">{setup.sharedKey}</code>
                <FormField label="Verification code" value={setupCode} onChange={(e) => setSetupCode(e.target.value)} error={problem?.kind === "validation" ? problem.fieldErrors.code?.[0] : undefined} />
                <button type="button" className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-400" disabled={working || !setupCode} onClick={() => runAction(async () => {
                  const response = await confirmMfaSetupAccount({ code: setupCode });
                  setRecoveryCodes(response.recoveryCodes);
                  setSetupCode("");
                  setSuccess("MFA enabled.");
                })}>Verify & enable MFA</button>
              </div>
            ) : null}
          </div>
        ) : (
          <div className="space-y-3">
            <FormField label="Authenticator code" value={disableCode} onChange={(e) => setDisableCode(e.target.value)} error={problem?.kind === "validation" ? problem.fieldErrors.code?.[0] : undefined} />
            <button type="button" className="rounded-lg border border-rose-700 px-4 py-2 text-sm font-semibold text-rose-200 hover:border-rose-500" disabled={working || !disableCode} onClick={() => runAction(async () => {
              await disableMfaAccount({ code: disableCode });
              setDisableCode("");
              setSuccess("MFA disabled.");
            })}>Disable MFA</button>
            <button type="button" className="rounded-lg border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 hover:border-slate-500" disabled={working} onClick={() => runAction(async () => {
              const response = await regenerateRecoveryCodesAccount();
              setRecoveryCodes(response.recoveryCodes);
              setSuccess("Recovery codes generated.");
            })}>Generate recovery codes</button>
          </div>
        )}

        {recoveryCodes.length > 0 ? <textarea readOnly value={codesText} className="min-h-[130px] w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 font-mono text-xs text-slate-100" /> : null}
      </div>
    </div>
  );
}
