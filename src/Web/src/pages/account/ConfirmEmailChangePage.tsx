import { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { confirmEmailChangeAccount } from "../../account/api";
import { mapErrorToProblem } from "../../account/errors";
import type { ParsedProblemResult } from "../../api/problemDetails";
import InlineAlert from "../../components/account/InlineAlert";
import AccountPageHeader from "../../components/account/AccountPageHeader";

export default function ConfirmEmailChangePage() {
  const [params] = useSearchParams();
  const userId = useMemo(() => params.get("userId") ?? "", [params]);
  const email = useMemo(() => params.get("email") ?? "", [params]);
  const token = useMemo(() => params.get("token") ?? "", [params]);

  const [working, setWorking] = useState(false);
  const [success, setSuccess] = useState("");
  const [problem, setProblem] = useState<ParsedProblemResult | null>(null);

  const onConfirm = async () => {
    setWorking(true);
    setSuccess("");
    setProblem(null);

    try {
      const response = await confirmEmailChangeAccount({ userId, newEmail: email, token });
      setSuccess(response.message ?? "Your email has been updated.");
    } catch (error) {
      setProblem(mapErrorToProblem(error, "Unable to confirm email change."));
    } finally {
      setWorking(false);
    }
  };

  return (
    <div className="mx-auto max-w-2xl">
      <AccountPageHeader title="Confirm email change" description="Complete your requested email change." />
      {success ? <InlineAlert kind="success" message={success} /> : null}
      {problem?.kind === "problem" ? <InlineAlert kind="error" message={`${problem.detail ?? "Request failed."}${problem.errorId ? ` (Error ID: ${problem.errorId})` : ""}`} /> : null}
      {problem?.kind === "validation" ? <InlineAlert kind="error" message={Object.values(problem.fieldErrors).flat()[0] ?? "Validation failed."} /> : null}
      <button type="button" onClick={onConfirm} disabled={!userId || !email || !token || working} className="mt-4 rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60">
        {working ? "Confirming..." : "Confirm email change"}
      </button>
      <div className="mt-4 text-sm">
        <Link className="text-indigo-300 hover:text-indigo-200" to={success ? "/account/email" : "/login"}>
          {success ? "Back to Account Email" : "Back to login"}
        </Link>
      </div>
    </div>
  );
}
