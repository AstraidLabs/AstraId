import { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import Alert from "../components/Alert";
import Card from "../components/Card";
import DiagnosticsPanel from "../components/DiagnosticsPanel";
import { confirmEmailChange } from "../api/authServer";
import { AppError } from "../api/errors";

const ConfirmEmailChange = () => {
  const [params] = useSearchParams();
  const userId = useMemo(() => params.get("userId") ?? "", [params]);
  const email = useMemo(() => params.get("email") ?? "", [params]);
  const token = useMemo(() => params.get("token") ?? "", [params]);

  const [error, setError] = useState<AppError | null>(null);
  const [success, setSuccess] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleConfirm = async () => {
    setError(null);
    setSuccess("");
    setIsSubmitting(true);
    try {
      const response = await confirmEmailChange({ userId, newEmail: email, token });
      setSuccess(response.message ?? "Your email has been updated.");
    } catch (err) {
      if (err && typeof err === "object" && "status" in err) {
        setError(err as AppError);
      } else {
        setError(new AppError({ status: 500, detail: "Unable to confirm email change." }));
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-6">
      <Card title="Confirm email change">
        <div className="flex flex-col gap-4">
          {error ? (
            <>
              <Alert variant="error">{error.detail ?? error.message}</Alert>
              <DiagnosticsPanel traceId={error.traceId} errorId={error.errorId} debug={error.debug} compact />
            </>
          ) : null}
          {success ? <Alert variant="success">{success}</Alert> : null}
          <p className="text-sm text-slate-300">Click the button below to confirm your new email address.</p>
          <button
            type="button"
            onClick={handleConfirm}
            disabled={!userId || !email || !token || isSubmitting}
            className="w-fit rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? "Confirming..." : "Confirm email change"}
          </button>
          <Link className="text-sm text-indigo-300 hover:text-indigo-200" to="/login">
            Back to login
          </Link>
        </div>
      </Card>
    </div>
  );
};

export default ConfirmEmailChange;
