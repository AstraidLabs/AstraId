import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { requestEmailChangeAccount } from "../../account/api";
import { mapErrorToProblem } from "../../account/errors";
import type { ParsedProblemResult } from "../../api/problemDetails";
import FormField from "../../components/account/FormField";
import InlineAlert from "../../components/account/InlineAlert";
import AccountPageHeader from "../../components/account/AccountPageHeader";

export default function EmailPage() {
  const [newEmail, setNewEmail] = useState("");
  const [currentPassword, setCurrentPassword] = useState("");
  const [working, setWorking] = useState(false);
  const [success, setSuccess] = useState("");
  const [problem, setProblem] = useState<ParsedProblemResult | null>(null);
  const navigate = useNavigate();

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setWorking(true);
    setProblem(null);
    setSuccess("");

    try {
      await requestEmailChangeAccount({ newEmail, currentPassword, returnUrl: "/account/security/email/confirm" });
      setCurrentPassword("");
      setSuccess("Check your email for a confirmation link.");
    } catch (error) {
      const parsed = mapErrorToProblem(error, "Unable to request email change.");
      if (parsed.status === 401 || parsed.status === 403) {
        navigate(`/login?returnUrl=${encodeURIComponent("/account/security/email")}`, { replace: true });
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
      <AccountPageHeader title="Email" description="Request a change for your account email address." />
      <form className="space-y-3 rounded-xl border border-slate-800 bg-slate-950/50 p-5" onSubmit={onSubmit}>
        {success ? <InlineAlert kind="success" message={success} /> : null}
        {problem?.kind === "problem" ? <InlineAlert kind="error" message={`${problem.detail ?? "Request failed."}${problem.errorId ? ` (Error ID: ${problem.errorId})` : ""}`} /> : null}
        {problem?.kind === "validation" ? <InlineAlert kind="error" message={Object.values(problem.fieldErrors).flat()[0] ?? "Validation failed."} /> : null}
        <FormField label="New email" type="email" value={newEmail} onChange={(e) => setNewEmail(e.target.value)} error={fieldErrors.newEmail?.[0]} autoComplete="email" />
        <FormField label="Current password" type="password" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} error={fieldErrors.currentPassword?.[0]} autoComplete="current-password" />
        <button type="submit" disabled={working} className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60">
          {working ? "Submitting..." : "Send confirmation link"}
        </button>
      </form>
    </div>
  );
}
