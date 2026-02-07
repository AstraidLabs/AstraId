import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { getOverview } from "../../account/api";
import type { SecurityOverviewResponse } from "../../api/authServer";
import type { ParsedProblemResult } from "../../api/problemDetails";
import { mapErrorToProblem } from "../../account/errors";
import InlineAlert from "../../components/account/InlineAlert";
import LoadingState from "../../components/account/LoadingState";
import PageHeader from "../../components/account/PageHeader";
import { LockIcon, MailIcon, MonitorIcon } from "../../components/account/Icons";

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
          navigate(`/login?returnUrl=${encodeURIComponent("/account/overview")}`, { replace: true });
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
      <PageHeader title="Overview" description="Your current security status and shortcuts." />
      {loading ? <LoadingState message="Loading overview..." /> : null}
      {problem?.kind === "problem" ? (
        <InlineAlert
          kind="error"
          message={`${problem.detail ?? problem.title ?? "Unable to load overview."}${problem.errorId ? ` (Error ID: ${problem.errorId})` : ""}`}
        />
      ) : null}
      {overview ? (
        <div className="space-y-4">
          <div className="grid gap-3 md:grid-cols-3">
            <div className="rounded-lg border border-slate-800 bg-slate-950/50 p-4">
              <p className="text-xs uppercase text-slate-400">Email</p>
              <p className="mt-1 text-sm text-white">{overview.email ?? "Not available"}</p>
              <p className="mt-2 text-xs text-slate-300">{overview.emailConfirmed ? "Confirmed" : "Not confirmed"}</p>
            </div>
            <div className="rounded-lg border border-slate-800 bg-slate-950/50 p-4">
              <p className="text-xs uppercase text-slate-400">MFA</p>
              <p className="mt-1 text-sm text-white">{overview.twoFactorEnabled ? "Enabled" : "Disabled"}</p>
              <p className="mt-2 text-xs text-slate-300">Recovery codes left: {overview.recoveryCodesLeft}</p>
            </div>
            <div className="rounded-lg border border-slate-800 bg-slate-950/50 p-4">
              <p className="text-xs uppercase text-slate-400">Username</p>
              <p className="mt-1 text-sm text-white">{overview.userName ?? "Not available"}</p>
            </div>
          </div>
          <div className="grid gap-3 md:grid-cols-3">
            <Link className="rounded-lg border border-slate-700 p-4 text-sm text-slate-100 hover:border-indigo-400" to="/account/password">
              <span className="mb-2 inline-block"><LockIcon /></span>Password
            </Link>
            <Link className="rounded-lg border border-slate-700 p-4 text-sm text-slate-100 hover:border-indigo-400" to="/account/email">
              <span className="mb-2 inline-block"><MailIcon /></span>Email
            </Link>
            <Link className="rounded-lg border border-slate-700 p-4 text-sm text-slate-100 hover:border-indigo-400" to="/account/sessions">
              <span className="mb-2 inline-block"><MonitorIcon /></span>Sessions
            </Link>
          </div>
        </div>
      ) : null}
    </div>
  );
}
