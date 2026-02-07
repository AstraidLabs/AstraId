import { useEffect, useMemo, useState } from "react";
import Alert from "../components/Alert";
import Card from "../components/Card";
import DiagnosticsPanel from "../components/DiagnosticsPanel";
import FieldError from "../components/FieldError";
import {
  changePassword,
  confirmMfaSetup,
  disableMfa,
  getMfaStatus,
  getSecurityOverview,
  regenerateRecoveryCodes,
  requestEmailChange,
  revokeOtherSessions,
  startMfaSetup,
  type MfaRecoveryCodesResponse,
  type MfaSetupResponse,
  type MfaStatus,
  type SecurityOverviewResponse
} from "../api/authServer";
import { AppError, type DiagnosticsDebug, type FieldErrors } from "../api/errors";
import { useAuthSession } from "../auth/useAuthSession";

const formatCodes = (codes: string[]) => codes.join("\n");

const AccountSecurity = () => {
  const { session, refresh } = useAuthSession();
  const [status, setStatus] = useState<MfaStatus | null>(null);
  const [overview, setOverview] = useState<SecurityOverviewResponse | null>(null);
  const [setupData, setSetupData] = useState<MfaSetupResponse | null>(null);
  const [qrCodeSvg, setQrCodeSvg] = useState<string | null>(null);
  const [setupCode, setSetupCode] = useState("");
  const [disableCode, setDisableCode] = useState("");
  const [codes, setCodes] = useState<string[]>([]);
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [signOutOthersOnPasswordChange, setSignOutOthersOnPasswordChange] = useState(true);
  const [newEmail, setNewEmail] = useState("");
  const [emailPassword, setEmailPassword] = useState("");
  const [sessionsPassword, setSessionsPassword] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<AppError | null>(null);
  const [actionSuccess, setActionSuccess] = useState("");
  const [actionError, setActionError] = useState("");
  const [actionDiagnostics, setActionDiagnostics] = useState<DiagnosticsDebug | undefined>(undefined);
  const [actionMeta, setActionMeta] = useState<{ traceId?: string; errorId?: string }>({});
  const [actionFieldErrors, setActionFieldErrors] = useState<FieldErrors>({});
  const [isWorking, setIsWorking] = useState(false);

  const isAuthenticated = session?.isAuthenticated ?? false;

  const codesText = useMemo(() => (codes.length ? formatCodes(codes) : ""), [codes]);

  const loadStatus = async () => {
    setLoading(true);
    setError(null);
    try {
      const [mfa, securityOverview] = await Promise.all([getMfaStatus(), getSecurityOverview()]);
      setStatus(mfa);
      setOverview(securityOverview);
    } catch (err) {
      if (err && typeof err === "object" && "status" in err) {
        setError(err as AppError);
      } else {
        setError(new AppError({ status: 500, detail: "Unable to load security data." }));
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

  const setActionFailure = (err: unknown, fallback: string) => {
    if (err && typeof err === "object" && "status" in err) {
      const appError = err as AppError;
      setActionError(appError.detail ?? appError.message);
      setActionDiagnostics(appError.debug);
      setActionMeta({ traceId: appError.traceId, errorId: appError.errorId });
      setActionFieldErrors(appError.fieldErrors ?? {});
    } else {
      setActionError(fallback);
    }
  };

  const clearActionState = () => {
    setActionSuccess("");
    setActionError("");
    setActionDiagnostics(undefined);
    setActionMeta({});
    setActionFieldErrors({});
  };

  const handleChangePassword = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    clearActionState();
    setIsWorking(true);
    try {
      const response = await changePassword({
        currentPassword,
        newPassword,
        confirmPassword,
        signOutOtherSessions: signOutOthersOnPasswordChange
      });
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setActionSuccess(response.message ?? "Password updated.");
      await refresh();
    } catch (err) {
      setActionFailure(err, "Unable to update password.");
    } finally {
      setIsWorking(false);
    }
  };

  const handleEmailChangeRequest = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    clearActionState();
    setIsWorking(true);
    try {
      const response = await requestEmailChange({
        newEmail,
        currentPassword: emailPassword,
        returnUrl: "/account/security"
      });
      setEmailPassword("");
      setActionSuccess(response.message ?? "If the email can be changed, you will receive a confirmation link.");
    } catch (err) {
      setActionFailure(err, "Unable to request email change.");
    } finally {
      setIsWorking(false);
    }
  };

  const handleRevokeOtherSessions = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    clearActionState();
    setIsWorking(true);
    try {
      const response = await revokeOtherSessions({ currentPassword: sessionsPassword });
      setSessionsPassword("");
      setActionSuccess(response.message ?? "Other sessions were signed out.");
      await refresh();
    } catch (err) {
      setActionFailure(err, "Unable to revoke sessions.");
    } finally {
      setIsWorking(false);
    }
  };

  const handleStartSetup = async () => {
    clearActionState();
    setIsWorking(true);
    try {
      const data = await startMfaSetup();
      setSetupData(data);
      setCodes([]);
      setQrCodeSvg(data.qrCodeSvg);
    } catch (err) {
      setActionFailure(err, "Unable to start MFA setup.");
    } finally {
      setIsWorking(false);
    }
  };

  const handleConfirmSetup = async () => {
    if (!setupCode.trim()) {
      setActionError("Enter the verification code from your authenticator app.");
      return;
    }
    clearActionState();
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
      setActionFailure(err, "Unable to verify the code.");
    } finally {
      setIsWorking(false);
    }
  };

  const handleRegenerateCodes = async () => {
    clearActionState();
    setIsWorking(true);
    try {
      const response: MfaRecoveryCodesResponse = await regenerateRecoveryCodes();
      setCodes(response.recoveryCodes);
      await loadStatus();
    } catch (err) {
      setActionFailure(err, "Unable to regenerate recovery codes.");
    } finally {
      setIsWorking(false);
    }
  };

  const handleDisable = async () => {
    if (!disableCode.trim()) {
      setActionError("Enter the verification code.");
      return;
    }
    clearActionState();
    setIsWorking(true);
    try {
      await disableMfa({ code: disableCode });
      setDisableCode("");
      setCodes([]);
      await loadStatus();
      await refresh();
    } catch (err) {
      setActionFailure(err, "Unable to disable MFA.");
    } finally {
      setIsWorking(false);
    }
  };

  if (!isAuthenticated) {
    return (
      <div className="mx-auto max-w-2xl">
        <Card title="Account security" description="You must be signed in to manage security settings.">
          <Alert variant="info">Sign in to continue.</Alert>
        </Card>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="mx-auto max-w-2xl">
        <Card title="Account security">
          <p className="text-sm text-slate-300">Loading security settings...</p>
        </Card>
      </div>
    );
  }

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-6">
      <Card title="Account security" description="Manage your password, email, sessions, and MFA.">
        {error ? (
          <div className="flex flex-col gap-3">
            <Alert variant="error">{error.detail ?? error.message}</Alert>
            <DiagnosticsPanel traceId={error.traceId} errorId={error.errorId} debug={error.debug} compact />
          </div>
        ) : null}
        {actionSuccess ? <Alert variant="success">{actionSuccess}</Alert> : null}
        {actionError ? (
          <div className="flex flex-col gap-3">
            <Alert variant="error">{actionError}</Alert>
            <DiagnosticsPanel traceId={actionMeta.traceId} errorId={actionMeta.errorId} debug={actionDiagnostics} compact />
          </div>
        ) : null}
        {overview ? (
          <div className="mt-4 grid gap-4 md:grid-cols-4">
            <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-4"><p className="text-xs uppercase tracking-wide text-slate-500">Email</p><p className="text-lg font-semibold text-white">{overview.email ?? "N/A"}</p></div>
            <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-4"><p className="text-xs uppercase tracking-wide text-slate-500">Email verified</p><p className="text-lg font-semibold text-white">{overview.emailConfirmed ? "Yes" : "No"}</p></div>
            <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-4"><p className="text-xs uppercase tracking-wide text-slate-500">MFA</p><p className="text-lg font-semibold text-white">{overview.twoFactorEnabled ? "Enabled" : "Disabled"}</p></div>
            <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-4"><p className="text-xs uppercase tracking-wide text-slate-500">Recovery codes left</p><p className="text-lg font-semibold text-white">{overview.recoveryCodesLeft}</p></div>
          </div>
        ) : null}
      </Card>

      <Card title="Change password">
        <form className="flex flex-col gap-3" onSubmit={handleChangePassword}>
          <label className="text-sm text-slate-200">Current password<input className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white" type="password" value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} autoComplete="current-password" /><FieldError message={actionFieldErrors.currentPassword?.[0]} /></label>
          <label className="text-sm text-slate-200">New password<input className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white" type="password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} autoComplete="new-password" /><FieldError message={actionFieldErrors.newPassword?.[0]} /></label>
          <label className="text-sm text-slate-200">Confirm password<input className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white" type="password" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} autoComplete="new-password" /><FieldError message={actionFieldErrors.confirmPassword?.[0]} /></label>
          <label className="flex items-center gap-2 text-sm text-slate-300"><input type="checkbox" checked={signOutOthersOnPasswordChange} onChange={(event) => setSignOutOthersOnPasswordChange(event.target.checked)} /> Sign out other sessions</label>
          <button type="submit" disabled={isWorking} className="w-fit rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60">Update password</button>
        </form>
      </Card>

      <Card title="Change email">
        <form className="flex flex-col gap-3" onSubmit={handleEmailChangeRequest}>
          <label className="text-sm text-slate-200">New email<input className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white" type="email" value={newEmail} onChange={(event) => setNewEmail(event.target.value)} autoComplete="email" /><FieldError message={actionFieldErrors.newEmail?.[0]} /></label>
          <label className="text-sm text-slate-200">Current password<input className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white" type="password" value={emailPassword} onChange={(event) => setEmailPassword(event.target.value)} autoComplete="current-password" /><FieldError message={actionFieldErrors.currentPassword?.[0]} /></label>
          <button type="submit" disabled={isWorking} className="w-fit rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60">Send confirmation link</button>
        </form>
      </Card>

      <Card title="Sessions">
        <form className="flex flex-col gap-3" onSubmit={handleRevokeOtherSessions}>
          <label className="text-sm text-slate-200">Current password<input className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white" type="password" value={sessionsPassword} onChange={(event) => setSessionsPassword(event.target.value)} autoComplete="current-password" /><FieldError message={actionFieldErrors.currentPassword?.[0]} /></label>
          <button type="submit" disabled={isWorking} className="w-fit rounded-lg border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500 disabled:cursor-not-allowed disabled:opacity-60">Sign out other sessions</button>
        </form>
      </Card>

      <Card title="MFA setup" description="Use an authenticator app.">
        {!status?.enabled ? (
          <div className="flex flex-col gap-4">
            <button type="button" onClick={handleStartSetup} disabled={isWorking} className="w-fit rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60">Start MFA setup</button>
            {setupData ? (
              <div className="grid gap-4 md:grid-cols-[200px,1fr]"><div className="rounded-lg border border-slate-800 bg-slate-950/40 p-4">{qrCodeSvg ? <div className="mx-auto h-40 w-40 text-white" aria-label="QR code for MFA" dangerouslySetInnerHTML={{ __html: qrCodeSvg }} /> : <p className="text-xs text-slate-400">Loading QR…</p>}</div><div className="flex flex-col gap-3 text-sm text-slate-300"><p>Scan the QR code or use the key:</p><code className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100">{setupData.sharedKey}</code><label className="text-sm text-slate-200">Verification code<input className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white" type="text" value={setupCode} onChange={(event) => setSetupCode(event.target.value)} /><FieldError message={actionFieldErrors.code?.[0]} /></label><button type="button" onClick={handleConfirmSetup} disabled={isWorking} className="w-fit rounded-lg bg-emerald-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60">Verify & enable MFA</button></div></div>
            ) : null}
          </div>
        ) : (
          <p className="text-sm text-slate-400">MFA is enabled.</p>
        )}
      </Card>

      <Card title="Recovery codes">
        {codes.length ? (
          <div className="flex flex-col gap-3"><Alert variant="warning">Recovery codes are shown only once. Store them securely.</Alert><textarea className="min-h-[140px] rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 font-mono text-sm text-slate-100" value={codesText} readOnly /></div>
        ) : (
          <p className="text-sm text-slate-400">Generate new recovery codes using the button below.</p>
        )}
        <div className="mt-4"><button type="button" onClick={handleRegenerateCodes} disabled={!status?.enabled || isWorking} className="rounded-lg border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500 disabled:cursor-not-allowed disabled:opacity-60">Generate new recovery codes</button></div>
      </Card>

      <Card title="Disable MFA">
        <div className="flex flex-col gap-3"><label className="text-sm text-slate-200">Verification code<input className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white" type="text" value={disableCode} onChange={(event) => setDisableCode(event.target.value)} /><FieldError message={actionFieldErrors.code?.[0]} /></label><button type="button" onClick={handleDisable} disabled={!status?.enabled || isWorking} className="w-fit rounded-lg bg-rose-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-rose-400 disabled:cursor-not-allowed disabled:opacity-60">Disable MFA</button></div>
      </Card>
    </div>
  );
};

export default AccountSecurity;
