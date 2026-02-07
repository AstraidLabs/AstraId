import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { revokeOtherSessionsAccount } from "../../account/api";
import { mapErrorToProblem } from "../../account/errors";
import type { ParsedProblemResult } from "../../api/problemDetails";
import FormField from "../../components/account/FormField";
import InlineAlert from "../../components/account/InlineAlert";
import AccountPageHeader from "../../components/account/AccountPageHeader";

export default function SessionsPage() {
  const [currentPassword, setCurrentPassword] = useState("");
  const [working, setWorking] = useState(false);
  const [success, setSuccess] = useState("");
  const [problem, setProblem] = useState<ParsedProblemResult | null>(null);
  const navigate = useNavigate();

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setWorking(true);
    setSuccess("");
    setProblem(null);

    try {
      await revokeOtherSessionsAccount({ currentPassword });
      setCurrentPassword("");
      setSuccess("Other sessions were signed out.");
    } catch (error) {
      const parsed = mapErrorToProblem(error, "Unable to revoke other sessions.");
      if (parsed.status === 401 || parsed.status === 403) {
        navigate(`/login?returnUrl=${encodeURIComponent("/account/sessions")}`, { replace: true });
        return;
      }
      setProblem(parsed);
    } finally {
      setWorking(false);
    }
  };

  const fieldErrors = problem?.kind === "validation" ? problem.fieldErrors : {};

  return (
    <div>
      <AccountPageHeader title="Sessions" description="Sign out all other active sessions." />
      <form className="space-y-3 rounded-xl border border-slate-800 bg-slate-950/50 p-5" onSubmit={onSubmit}>
        {success ? <InlineAlert kind="success" message={success} /> : null}
        {problem?.kind === "problem" ? <InlineAlert kind="error" message={`${problem.detail ?? "Request failed."}${problem.errorId ? ` (Error ID: ${problem.errorId})` : ""}`} /> : null}
        {problem?.kind === "validation" ? <InlineAlert kind="error" message={Object.values(problem.fieldErrors).flat()[0] ?? "Validation failed."} /> : null}
        <FormField label="Current password" type="password" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} error={fieldErrors.currentPassword?.[0]} autoComplete="current-password" />
        <button type="submit" disabled={working} className="rounded-lg border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 hover:border-slate-500 disabled:opacity-60">
          {working ? "Signing out..." : "Sign out other sessions"}
        </button>
      </form>
    </div>
  );
}
