import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { confirmMfaSetupAccount, disableMfaAccount, getMfaStatusAccount, regenerateRecoveryCodesAccount, startMfaSetupAccount } from "../../account/api";
import type { MfaSetupResponse, MfaStatus } from "../../api/authServer";
import type { ParsedProblemResult } from "../../api/problemDetails";
import { mapErrorToProblem } from "../../account/errors";
import FormField from "../../components/account/FormField";
import InlineAlert from "../../components/account/InlineAlert";
import LoadingState from "../../components/account/LoadingState";
import AccountPageHeader from "../../components/account/AccountPageHeader";
import { useLanguage } from "../../i18n/LanguageProvider";

export default function MfaPage() {
  const { t } = useLanguage();
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
      const parsed = mapErrorToProblem(error, t("account.mfa.loadError"));
      if (parsed.status === 401 || parsed.status === 403) {
        navigate(`/login?returnUrl=${encodeURIComponent("/account/security/mfa")}`, { replace: true });
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
      setProblem(mapErrorToProblem(error, t("account.mfa.actionError")));
    } finally {
      setWorking(false);
    }
  };

  const codesText = useMemo(() => recoveryCodes.join("\n"), [recoveryCodes]);

  return (
    <div>
      <AccountPageHeader title={t("account.mfa.title")} description={t("account.mfa.description")} />
      {loading ? <LoadingState message={t("account.mfa.loading")} /> : null}
      {success ? <InlineAlert kind="success" message={success} /> : null}
      {problem?.kind === "problem" ? <InlineAlert kind="error" message={`${problem.detail ?? t("common.requestFailed")}${problem.errorId ? ` (Error ID: ${problem.errorId})` : ""}`} /> : null}
      <div className="mt-4 space-y-4">
        <div className="rounded-lg border border-slate-800 bg-slate-950/50 p-4 text-sm text-slate-200">
          <p>{t("account.mfa.status")}: <span className="font-semibold text-white">{status?.enabled ? t("account.overview.enabled") : t("account.overview.disabled")}</span></p>
          <p className="mt-1">{t("account.mfa.recoveryCodesLeft")}: {status?.recoveryCodesLeft ?? 0}</p>
        </div>

        {!status?.enabled ? (
          <div className="space-y-3">
            <button type="button" className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60" disabled={working} onClick={() => runAction(async () => {
              const response = await startMfaSetupAccount();
              setSetup(response);
            })}>{t("account.mfa.start")}</button>
            {setup ? (
              <div className="rounded-lg border border-slate-800 bg-slate-950/50 p-4">
                <p className="text-sm text-slate-300">{t("account.mfa.sharedKey")}</p>
                <code className="mt-2 block rounded bg-slate-900 px-2 py-2 text-xs text-slate-100">{setup.sharedKey}</code>
                <FormField label={t("mfa.code")} value={setupCode} onChange={(e) => setSetupCode(e.target.value)} error={problem?.kind === "validation" ? problem.fieldErrors.code?.[0] : undefined} />
                <button type="button" className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-400" disabled={working || !setupCode} onClick={() => runAction(async () => {
                  const response = await confirmMfaSetupAccount({ code: setupCode });
                  setRecoveryCodes(response.recoveryCodes);
                  setSetupCode("");
                  setSuccess(t("account.mfa.enabled"));
                })}>{t("account.mfa.verifyEnable")}</button>
              </div>
            ) : null}
          </div>
        ) : (
          <div className="space-y-3">
            <FormField label={t("account.mfa.authenticatorCode")} value={disableCode} onChange={(e) => setDisableCode(e.target.value)} error={problem?.kind === "validation" ? problem.fieldErrors.code?.[0] : undefined} />
            <button type="button" className="rounded-lg border border-rose-700 px-4 py-2 text-sm font-semibold text-rose-200 hover:border-rose-500" disabled={working || !disableCode} onClick={() => runAction(async () => {
              await disableMfaAccount({ code: disableCode });
              setDisableCode("");
              setSuccess(t("account.mfa.disabledMsg"));
            })}>{t("account.mfa.disable")}</button>
            <button type="button" className="rounded-lg border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 hover:border-slate-500" disabled={working} onClick={() => runAction(async () => {
              const response = await regenerateRecoveryCodesAccount();
              setRecoveryCodes(response.recoveryCodes);
              setSuccess(t("account.mfa.codesGenerated"));
            })}>{t("account.mfa.generateCodes")}</button>
          </div>
        )}

        {recoveryCodes.length > 0 ? <textarea readOnly value={codesText} className="min-h-[130px] w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 font-mono text-xs text-slate-100" /> : null}
      </div>
    </div>
  );
}
