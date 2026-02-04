import { useEffect, useMemo, useState } from "react";
import Alert from "../components/Alert";
import Card from "../components/Card";
import DiagnosticsPanel from "../components/DiagnosticsPanel";
import FieldError from "../components/FieldError";
import {
  confirmMfaSetup,
  disableMfa,
  getMfaStatus,
  regenerateRecoveryCodes,
  startMfaSetup,
  type MfaRecoveryCodesResponse,
  type MfaSetupResponse,
  type MfaStatus
} from "../api/authServer";
import { AppError, type DiagnosticsDebug, type FieldErrors } from "../api/errors";
import { useAuthSession } from "../auth/useAuthSession";

const formatCodes = (codes: string[]) => codes.join("\n");

const AccountSecurity = () => {
  const { session, refresh } = useAuthSession();
  const [status, setStatus] = useState<MfaStatus | null>(null);
  const [setupData, setSetupData] = useState<MfaSetupResponse | null>(null);
  const [qrCodeSvg, setQrCodeSvg] = useState<string | null>(null);
  const [setupCode, setSetupCode] = useState("");
  const [disableCode, setDisableCode] = useState("");
  const [codes, setCodes] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<AppError | null>(null);
  const [actionError, setActionError] = useState("");
  const [actionDiagnostics, setActionDiagnostics] = useState<DiagnosticsDebug | undefined>(
    undefined
  );
  const [actionMeta, setActionMeta] = useState<{ traceId?: string; errorId?: string }>({});
  const [actionFieldErrors, setActionFieldErrors] = useState<FieldErrors>({});
  const [isWorking, setIsWorking] = useState(false);

  const isAuthenticated = session?.isAuthenticated ?? false;

  const codesText = useMemo(
    () => (codes.length ? formatCodes(codes) : ""),
    [codes]
  );

  const loadStatus = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getMfaStatus();
      setStatus(data);
    } catch (err) {
      if (err && typeof err === "object" && "status" in err) {
        setError(err as AppError);
      } else {
        setError(new AppError({ status: 500, detail: "Unable to load MFA status." }));
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!isAuthenticated) {
      setLoading(false);
      return;
    }
    void loadStatus();
  }, [isAuthenticated]);

  const handleStartSetup = async () => {
    setActionError("");
    setActionDiagnostics(undefined);
    setActionMeta({});
    setActionFieldErrors({});
    setIsWorking(true);
    try {
      const data = await startMfaSetup();
      setSetupData(data);
      setCodes([]);
      setQrCodeSvg(data.qrCodeSvg);
    } catch (err) {
      if (err && typeof err === "object" && "status" in err) {
        const appError = err as AppError;
        setActionError(appError.detail ?? appError.message);
        setActionDiagnostics(appError.debug);
        setActionMeta({ traceId: appError.traceId, errorId: appError.errorId });
        setActionFieldErrors(appError.fieldErrors ?? {});
      } else {
        setActionError("Unable to start MFA setup.");
      }
    } finally {
      setIsWorking(false);
    }
  };

  const handleConfirmSetup = async () => {
    if (!setupCode.trim()) {
      setActionError("Enter the verification code from your authenticator app.");
      setActionDiagnostics(undefined);
      setActionMeta({});
      setActionFieldErrors({});
      return;
    }
    setActionError("");
    setActionDiagnostics(undefined);
    setActionMeta({});
    setActionFieldErrors({});
    setIsWorking(true);
    try {
      const response = await confirmMfaSetup({ code: setupCode });
      setCodes(response.recoveryCodes);
      setSetupCode("");
      setSetupData(null);
      setQrCodeSvg(null);
      await loadStatus();
      await refresh();
    } catch (err) {
      if (err && typeof err === "object" && "status" in err) {
        const appError = err as AppError;
        setActionError(appError.detail ?? appError.message);
        setActionDiagnostics(appError.debug);
        setActionMeta({ traceId: appError.traceId, errorId: appError.errorId });
        setActionFieldErrors(appError.fieldErrors ?? {});
      } else {
        setActionError("Unable to verify the code.");
      }
    } finally {
      setIsWorking(false);
    }
  };

  const handleRegenerateCodes = async () => {
    setActionError("");
    setActionDiagnostics(undefined);
    setActionMeta({});
    setActionFieldErrors({});
    setIsWorking(true);
    try {
      const response: MfaRecoveryCodesResponse = await regenerateRecoveryCodes();
      setCodes(response.recoveryCodes);
      await loadStatus();
    } catch (err) {
      if (err && typeof err === "object" && "status" in err) {
        const appError = err as AppError;
        setActionError(appError.detail ?? appError.message);
        setActionDiagnostics(appError.debug);
        setActionMeta({ traceId: appError.traceId, errorId: appError.errorId });
        setActionFieldErrors(appError.fieldErrors ?? {});
      } else {
        setActionError("Unable to regenerate recovery codes.");
      }
    } finally {
      setIsWorking(false);
    }
  };

  const handleDisable = async () => {
    if (!disableCode.trim()) {
      setActionError("Enter the verification code.");
      setActionDiagnostics(undefined);
      setActionMeta({});
      setActionFieldErrors({});
      return;
    }
    setActionError("");
    setActionDiagnostics(undefined);
    setActionMeta({});
    setActionFieldErrors({});
    setIsWorking(true);
    try {
      await disableMfa({ code: disableCode });
      setDisableCode("");
      setCodes([]);
      await loadStatus();
      await refresh();
    } catch (err) {
      if (err && typeof err === "object" && "status" in err) {
        const appError = err as AppError;
        setActionError(appError.detail ?? appError.message);
        setActionDiagnostics(appError.debug);
        setActionMeta({ traceId: appError.traceId, errorId: appError.errorId });
        setActionFieldErrors(appError.fieldErrors ?? {});
      } else {
        setActionError("Unable to disable MFA.");
      }
    } finally {
      setIsWorking(false);
    }
  };

  if (!isAuthenticated) {
    return (
      <div className="mx-auto max-w-2xl">
        <Card
          title="Account security"
          description="You must be signed in to manage MFA."
        >
          <Alert variant="info">Sign in to continue setting up MFA.</Alert>
        </Card>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="mx-auto max-w-2xl">
        <Card title="Account security">
          <p className="text-sm text-slate-300">Loading MFA settings...</p>
        </Card>
      </div>
    );
  }

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-6">
      <Card
        title="Account security"
        description="Manage MFA with an authenticator app and recovery codes."
      >
        {error ? (
          <div className="flex flex-col gap-3">
            <Alert variant="error">{error.detail ?? error.message}</Alert>
            {error.status === 401 ? (
              <a className="text-sm text-indigo-300 hover:text-indigo-200" href="/login">
                Sign in again
              </a>
            ) : null}
            <DiagnosticsPanel
              traceId={error.traceId}
              errorId={error.errorId}
              debug={error.debug}
              compact
            />
          </div>
        ) : null}
        {status ? (
          <div className="mt-4 grid gap-4 md:grid-cols-3">
            <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-4">
              <p className="text-xs uppercase tracking-wide text-slate-500">
                MFA status
              </p>
              <p className="text-lg font-semibold text-white">
                {status.enabled ? "Enabled" : "Disabled"}
              </p>
            </div>
            <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-4">
              <p className="text-xs uppercase tracking-wide text-slate-500">
                Recovery codes
              </p>
              <p className="text-lg font-semibold text-white">
                {status.recoveryCodesLeft}
              </p>
              <p className="text-xs text-slate-400">Remaining codes</p>
            </div>
            <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-4">
              <p className="text-xs uppercase tracking-wide text-slate-500">
                Authenticator key
              </p>
              <p className="text-lg font-semibold text-white">
                {status.hasAuthenticatorKey ? "Generated" : "Not generated"}
              </p>
            </div>
          </div>
        ) : null}
      </Card>

      <Card
        title="MFA setup"
        description="Use an authenticator app (Google Authenticator, Authy, Microsoft Authenticator, etc.)."
      >
        {actionError ? (
          <div className="flex flex-col gap-3">
            <Alert variant="error">{actionError}</Alert>
            <DiagnosticsPanel
              traceId={actionMeta.traceId}
              errorId={actionMeta.errorId}
              debug={actionDiagnostics}
              compact
            />
          </div>
        ) : null}
        {!status?.enabled ? (
          <div className="flex flex-col gap-4">
            <button
              type="button"
              onClick={handleStartSetup}
              disabled={isWorking}
              className="w-fit rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60"
            >
              Start MFA setup
            </button>
            {setupData ? (
              <div className="grid gap-4 md:grid-cols-[200px,1fr]">
                <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-4">
                  {qrCodeSvg ? (
                    <div
                      className="mx-auto h-40 w-40 text-white"
                      aria-label="QR code for MFA"
                      dangerouslySetInnerHTML={{ __html: qrCodeSvg }}
                    />
                  ) : (
                    <p className="text-xs text-slate-400">Loading QR…</p>
                  )}
                </div>
                <div className="flex flex-col gap-3 text-sm text-slate-300">
                  <p>
                    Scan the QR code in your authenticator app. If you cannot scan,
                    use this key instead:
                  </p>
                  <code className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100">
                    {setupData.sharedKey}
                  </code>
                  <label className="text-sm text-slate-200">
                    Verification code
                    <input
                      className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
                      type="text"
                      value={setupCode}
                      onChange={(event) => setSetupCode(event.target.value)}
                    />
                    <FieldError message={actionFieldErrors.code?.[0]} />
                  </label>
                  <button
                    type="button"
                    onClick={handleConfirmSetup}
                    disabled={isWorking}
                    className="w-fit rounded-lg bg-emerald-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Verify & enable MFA
                  </button>
                </div>
              </div>
            ) : (
              <p className="text-sm text-slate-400">
                After starting, you will see a QR code and key. MFA is enabled after
                verifying the code from your app.
              </p>
            )}
          </div>
        ) : (
          <p className="text-sm text-slate-400">
            MFA is enabled. Disable it first if you need to reconfigure.
          </p>
        )}
      </Card>

      <Card
        title="Recovery codes"
        description="Use recovery codes when you cannot access your authenticator app."
      >
        {codes.length ? (
          <div className="flex flex-col gap-3">
            <Alert variant="warning">
              Recovery codes are shown only once. Store them securely.
            </Alert>
            <textarea
              className="min-h-[140px] rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 font-mono text-sm text-slate-100"
              value={codesText}
              readOnly
            />
          </div>
        ) : (
          <p className="text-sm text-slate-400">
            Generate new recovery codes using the button below.
          </p>
        )}
        <div className="mt-4">
          <button
            type="button"
            onClick={handleRegenerateCodes}
            disabled={!status?.enabled || isWorking}
            className="rounded-lg border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500 disabled:cursor-not-allowed disabled:opacity-60"
          >
            Generate new recovery codes
          </button>
        </div>
      </Card>

      <Card
        title="Disable MFA"
        description="To disable MFA, verify the current code from your authenticator app."
      >
        <div className="flex flex-col gap-3">
          <label className="text-sm text-slate-200">
            Verification code
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="text"
              value={disableCode}
              onChange={(event) => setDisableCode(event.target.value)}
            />
            <FieldError message={actionFieldErrors.code?.[0]} />
          </label>
          <button
            type="button"
            onClick={handleDisable}
            disabled={!status?.enabled || isWorking}
            className="w-fit rounded-lg bg-rose-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-rose-400 disabled:cursor-not-allowed disabled:opacity-60"
          >
            Disable MFA
          </button>
        </div>
      </Card>
    </div>
  );
};

export default AccountSecurity;
