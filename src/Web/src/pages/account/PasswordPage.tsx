import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { changePasswordAccount } from "../../account/api";
import { mapErrorToProblem } from "../../account/errors";
import type { ParsedProblemResult } from "../../api/problemDetails";
import FormField from "../../components/account/FormField";
import InlineAlert from "../../components/account/InlineAlert";
import AccountPageHeader from "../../components/account/AccountPageHeader";

export default function PasswordPage() {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [signOutOthers, setSignOutOthers] = useState(true);
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
      await changePasswordAccount({ currentPassword, newPassword, confirmPassword, signOutOtherSessions: signOutOthers });
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setSuccess("Password updated.");
    } catch (error) {
      const parsed = mapErrorToProblem(error, "Unable to update password.");
      if (parsed.status === 401 || parsed.status === 403) {
        navigate(`/login?returnUrl=${encodeURIComponent("/account/password")}`, { replace: true });
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
      <AccountPageHeader title="Password" description="Change your password and optionally sign out other sessions." />
      <form className="space-y-3 rounded-xl border border-slate-800 bg-slate-950/50 p-5" onSubmit={onSubmit}>
        {success ? <InlineAlert kind="success" message={success} /> : null}
        {problem?.kind === "problem" ? <InlineAlert kind="error" message={`${problem.detail ?? "Request failed."}${problem.errorId ? ` (Error ID: ${problem.errorId})` : ""}`} /> : null}
        {problem?.kind === "validation" ? <InlineAlert kind="error" message={Object.values(problem.fieldErrors).flat()[0] ?? "Validation failed."} /> : null}
        <FormField label="Current password" type="password" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} error={fieldErrors.currentPassword?.[0]} autoComplete="current-password" />
        <FormField label="New password" type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} error={fieldErrors.newPassword?.[0]} autoComplete="new-password" />
        <FormField label="Confirm password" type="password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} error={fieldErrors.confirmPassword?.[0]} autoComplete="new-password" />
        <label className="flex items-center gap-2 text-sm text-slate-300">
          <input type="checkbox" checked={signOutOthers} onChange={(e) => setSignOutOthers(e.target.checked)} />
          Sign out other sessions
        </label>
        <button type="submit" disabled={working} className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60">
          {working ? "Updating..." : "Update password"}
        </button>
      </form>
    </div>
  );
}
