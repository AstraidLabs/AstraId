import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { getOverview } from "../../account/api";
import type { SecurityOverviewResponse } from "../../api/authServer";
import type { ParsedProblemResult } from "../../api/problemDetails";
import { mapErrorToProblem } from "../../account/errors";
import InlineAlert from "../../components/account/InlineAlert";
import AccountPageHeader from "../../components/account/AccountPageHeader";
import { LockIcon, MailIcon, MonitorIcon, ShieldIcon } from "../../components/account/Icons";

const statusCardClass = "rounded-xl border border-slate-800 bg-slate-950/50 p-4";

export default function OverviewPage() {
  const [overview, setOverview] = useState<SecurityOverviewResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [problem, setProblem] = useState<ParsedProblemResult | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const run = async () => {
      try {
        setProblem(null);
        setLoading(true);
        const data = await getOverview();
        setOverview(data);
      } catch (error) {
        const parsed = mapErrorToProblem(error, "Unable to load account overview.");
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
        title="Overview"
        description="Monitor your account security and jump straight to self-service actions."
      />

      {problem?.kind === "problem" ? (
        <InlineAlert
          kind="error"
          message={`${problem.detail ?? problem.title ?? "Unable to load overview."}${problem.errorId ? ` (Error ID: ${problem.errorId})` : ""}`}
        />
      ) : null}

      {loading ? (
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }, (_, index) => (
            <div key={index} className="h-28 animate-pulse rounded-xl border border-slate-800 bg-slate-900/50" />
          ))}
        </div>
      ) : null}

      {overview ? (
        <div className="space-y-5">
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            <div className={statusCardClass}>
              <p className="text-xs uppercase tracking-wide text-slate-400">Email verification</p>
              <p className="mt-2 text-lg font-semibold text-white">{overview.emailConfirmed ? "Verified" : "Verification required"}</p>
              <p className="mt-1 text-sm text-slate-400">{overview.email ?? "No email available"}</p>
            </div>
            <div className={statusCardClass}>
              <p className="text-xs uppercase tracking-wide text-slate-400">MFA protection</p>
              <p className="mt-2 text-lg font-semibold text-white">{overview.twoFactorEnabled ? "Enabled" : "Disabled"}</p>
              <p className="mt-1 text-sm text-slate-400">Recovery codes left: {overview.recoveryCodesLeft}</p>
            </div>
            <div className={statusCardClass}>
              <p className="text-xs uppercase tracking-wide text-slate-400">Sessions</p>
              <p className="mt-2 text-lg font-semibold text-white">Managed on demand</p>
              <p className="mt-1 text-sm text-slate-400">Sign out from all other devices in one step.</p>
            </div>
            <div className={statusCardClass}>
              <p className="text-xs uppercase tracking-wide text-slate-400">Security activity</p>
              <p className="mt-2 text-lg font-semibold text-white">Coming soon</p>
              <p className="mt-1 text-sm text-slate-400">Detailed event timeline is planned for a later release.</p>
            </div>
          </div>

          <div>
            <h3 className="mb-3 text-sm font-semibold uppercase tracking-[0.2em] text-slate-500">Quick actions</h3>
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
              <Link className="rounded-xl border border-slate-700 p-4 text-sm text-slate-100 transition hover:border-indigo-400" to="/account/password">
                <span className="mb-2 inline-flex rounded-lg bg-slate-800 p-2"><LockIcon /></span>
                <p className="font-medium">Change password</p>
              </Link>
              <Link className="rounded-xl border border-slate-700 p-4 text-sm text-slate-100 transition hover:border-indigo-400" to="/account/email">
                <span className="mb-2 inline-flex rounded-lg bg-slate-800 p-2"><MailIcon /></span>
                <p className="font-medium">Update email</p>
              </Link>
              <Link className="rounded-xl border border-slate-700 p-4 text-sm text-slate-100 transition hover:border-indigo-400" to="/account/mfa">
                <span className="mb-2 inline-flex rounded-lg bg-slate-800 p-2"><ShieldIcon /></span>
                <p className="font-medium">Manage MFA</p>
              </Link>
              <Link className="rounded-xl border border-slate-700 p-4 text-sm text-slate-100 transition hover:border-indigo-400" to="/account/sessions">
                <span className="mb-2 inline-flex rounded-lg bg-slate-800 p-2"><MonitorIcon /></span>
                <p className="font-medium">Sign out other sessions</p>
              </Link>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
