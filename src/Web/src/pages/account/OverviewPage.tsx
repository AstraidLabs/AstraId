import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { getMfaStatusAccount, getOverview } from "../../account/api";
import type { MfaStatus, SecurityOverviewResponse } from "../../api/authServer";
import type { ParsedProblemResult } from "../../api/problemDetails";
import { mapErrorToProblem } from "../../account/errors";
import InlineAlert from "../../components/account/InlineAlert";
import AccountPageHeader from "../../components/account/AccountPageHeader";
import { LockIcon, MailIcon, MonitorIcon, ShieldIcon, UserIcon } from "../../components/account/Icons";

const statusCardClass = "rounded-xl border border-slate-800 bg-slate-950/50 p-4";
const actionCardClass = "rounded-xl border border-slate-700 p-4 text-sm text-slate-100 transition hover:border-indigo-400";

export default function OverviewPage() {
  const [overview, setOverview] = useState<SecurityOverviewResponse | null>(null);
  const [mfaStatus, setMfaStatus] = useState<MfaStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [problem, setProblem] = useState<ParsedProblemResult | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const run = async () => {
      try {
        setProblem(null);
        setLoading(true);
        const [overviewData, mfaData] = await Promise.all([
          getOverview(),
          getMfaStatusAccount().catch(() => null)
        ]);
        setOverview(overviewData);
        setMfaStatus(mfaData);
      } catch (error) {
        const parsed = mapErrorToProblem(error, "Unable to load account dashboard.");
        if (parsed.status === 401 || parsed.status === 403) {
          navigate(`/login?returnUrl=${encodeURIComponent("/account")}`, { replace: true });
          return;
        }
        setProblem(parsed);
      } finally {
        setLoading(false);
      }
    };

    void run();
  }, [navigate]);

  return (
    <div>
      <AccountPageHeader
        title="Account"
        description="Enterprise self-service hub for your profile, credentials, and security controls."
      />

      {problem?.kind === "problem" ? (
        <InlineAlert
          kind="error"
          message={`${problem.detail ?? problem.title ?? "Unable to load account dashboard."}${problem.errorId ? ` (Error ID: ${problem.errorId})` : ""}`}
        />
      ) : null}

      {loading ? (
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 3 }, (_, index) => (
            <div key={index} className="h-28 animate-pulse rounded-xl border border-slate-800 bg-slate-900/50" />
          ))}
        </div>
      ) : null}

      {overview ? (
        <div className="space-y-5">
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            <div className={statusCardClass}>
              <p className="text-xs uppercase tracking-wide text-slate-400">Username</p>
              <p className="mt-2 text-lg font-semibold text-white">{overview.userName ?? "Not set"}</p>
            </div>
            <div className={statusCardClass}>
              <p className="text-xs uppercase tracking-wide text-slate-400">Email</p>
              <p className="mt-2 text-lg font-semibold text-white">{overview.emailConfirmed ? "Verified" : "Verification required"}</p>
              <p className="mt-1 text-sm text-slate-400">{overview.email ?? "No email available"}</p>
            </div>
            <div className={statusCardClass}>
              <p className="text-xs uppercase tracking-wide text-slate-400">MFA</p>
              <p className="mt-2 text-lg font-semibold text-white">
                {mfaStatus ? (mfaStatus.enabled ? "Enabled" : "Disabled") : "Unknown"}
              </p>
              <p className="mt-1 text-sm text-slate-400">
                {mfaStatus ? `Recovery codes left: ${mfaStatus.recoveryCodesLeft}` : "Open MFA page for details."}
              </p>
            </div>
          </div>

          <div>
            <h3 className="mb-3 text-sm font-semibold uppercase tracking-[0.2em] text-slate-500">Self-service</h3>
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
              <Link className={actionCardClass} to="/account/profile">
                <span className="mb-2 inline-flex rounded-lg bg-slate-800 p-2"><UserIcon /></span>
                <p className="font-medium">Profile</p>
              </Link>
              <Link className={actionCardClass} to="/account/password">
                <span className="mb-2 inline-flex rounded-lg bg-slate-800 p-2"><LockIcon /></span>
                <p className="font-medium">Password</p>
              </Link>
              <Link className={actionCardClass} to="/account/email">
                <span className="mb-2 inline-flex rounded-lg bg-slate-800 p-2"><MailIcon /></span>
                <p className="font-medium">Email</p>
              </Link>
              <Link className={actionCardClass} to="/account/mfa">
                <span className="mb-2 inline-flex rounded-lg bg-slate-800 p-2"><ShieldIcon /></span>
                <p className="font-medium">MFA</p>
              </Link>
              <Link className={actionCardClass} to="/account/sessions">
                <span className="mb-2 inline-flex rounded-lg bg-slate-800 p-2"><MonitorIcon /></span>
                <p className="font-medium">Sessions</p>
              </Link>
              <Link className={actionCardClass} to="/account/security-events">
                <span className="mb-2 inline-flex rounded-lg bg-slate-800 p-2"><ShieldIcon /></span>
                <p className="font-medium">Security events</p>
              </Link>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
