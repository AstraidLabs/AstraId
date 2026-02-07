import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { getMfaStatusAccount, getOverview } from "../../account/api";
import type { MfaStatus } from "../../api/authServer";
import type { MeSummary } from "../../account/api";
import type { ParsedProblemResult } from "../../api/problemDetails";
import { mapErrorToProblem } from "../../account/errors";
import InlineAlert from "../../components/account/InlineAlert";
import AccountPageHeader from "../../components/account/AccountPageHeader";
import { accountCardIconClass, accountIcons, securityItems } from "../../ui/accountIcons";

export default function OverviewPage() {
  const [overview, setOverview] = useState<MeSummary | null>(null);
  const [mfaStatus, setMfaStatus] = useState<MfaStatus | null>(null);
  const [problem, setProblem] = useState<ParsedProblemResult | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    (async () => {
      try {
        const [overviewData, mfaData] = await Promise.all([getOverview(), getMfaStatusAccount().catch(() => null)]);
        setOverview(overviewData);
        setMfaStatus(mfaData);
      } catch (error) {
        const parsed = mapErrorToProblem(error, "Unable to load account dashboard.");
        if (parsed.status === 401 || parsed.status === 403) return navigate(`/login?returnUrl=${encodeURIComponent("/account")}`, { replace: true });
        setProblem(parsed);
      }
    })();
  }, [navigate]);

  const ProfileIcon = accountIcons.profile;
  const SecurityIcon = accountIcons.security;

  return (
    <div className="space-y-5">
      <AccountPageHeader title="Profile" description="Your self-service account dashboard." />
      {problem?.kind === "problem" && <InlineAlert kind="error" message={problem.detail ?? problem.title ?? "Unable to load account dashboard."} />}

      <div className="grid gap-3 md:grid-cols-3">
        <div className="rounded-xl border border-slate-800 bg-slate-950/50 p-4"><p className="text-xs text-slate-400">Email</p><p className="mt-2 font-semibold text-white">{overview?.email ?? "Unknown"}</p></div>
        <div className="rounded-xl border border-slate-800 bg-slate-950/50 p-4"><p className="text-xs text-slate-400">Email status</p><p className="mt-2 font-semibold text-white">{overview?.emailConfirmed ? "Verified" : "Verification required"}</p></div>
        <div className="rounded-xl border border-slate-800 bg-slate-950/50 p-4"><p className="text-xs text-slate-400">MFA</p><p className="mt-2 font-semibold text-white">{mfaStatus?.enabled ? "Enabled" : "Disabled"}</p></div>
      </div>

      <div className="grid gap-3 md:grid-cols-2">
        <Link to="/account" className="rounded-xl border border-slate-700 p-4 hover:border-indigo-400"><ProfileIcon className={`${accountCardIconClass} mb-2 text-indigo-300`} /><p className="font-semibold text-white">Profile</p><p className="text-sm text-slate-400">Review your account identity details.</p></Link>
        <Link to="/account/security" className="rounded-xl border border-slate-700 p-4 hover:border-indigo-400"><SecurityIcon className={`${accountCardIconClass} mb-2 text-indigo-300`} /><p className="font-semibold text-white">Security</p><p className="text-sm text-slate-400">Open enterprise security controls and activity pages.</p></Link>
        {securityItems.map((item) => { const Icon = item.icon; return <Link key={item.key} to={item.to} className="rounded-xl border border-slate-700 p-4 hover:border-indigo-400"><Icon className={`${accountCardIconClass} mb-2 text-indigo-300`} /><p className="font-semibold text-white">{item.label}</p><p className="text-sm text-slate-400">{item.description}</p></Link>; })}
      </div>
    </div>
  );
}
