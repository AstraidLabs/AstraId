import { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import SignedInInfo from "../components/SignedInInfo";
import { useAuthSession } from "../auth/useAuthSession";
import Alert from "../components/Alert";
import Card from "../components/Card";
import DiagnosticsPanel from "../components/DiagnosticsPanel";
import FieldError from "../components/FieldError";
import { resetPassword } from "../api/authServer";
import { AppError, type FieldErrors } from "../api/errors";

const ResetPassword = () => {
  const { status } = useAuthSession();
  const [params] = useSearchParams();
  const initialEmail = useMemo(() => params.get("email") ?? "", [params]);
  const initialToken = useMemo(() => params.get("token") ?? "", [params]);

  const [email, setEmail] = useState(initialEmail);
  const [token, setToken] = useState(initialToken);
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<AppError | null>(null);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [success, setSuccess] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setFieldErrors({});
    setSuccess("");
    setIsSubmitting(true);

    try {
      await resetPassword({
        email,
        token,
        newPassword,
        confirmPassword
      });
      setSuccess("Your password was updated. Please sign in again.");
    } catch (err) {
      if (err && typeof err === "object" && "status" in err) {
        const appError = err as AppError;
        setError(appError);
        setFieldErrors(appError.fieldErrors ?? {});
      } else {
        setError(new AppError({ status: 500, detail: "Unable to reset password." }));
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  if (status === "authenticated") {
    return (
      <SignedInInfo
        title="Reset password"
        message="You are already signed in. Reset password links are intended for signed-out recovery flows."
      />
    );
  }

  return (
    <div className="mx-auto max-w-md">
      <Card title="Set a new password">
        <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
          {error ? (
            <div className="flex flex-col gap-3">
              <Alert variant="error">{error.detail ?? error.message}</Alert>
              <DiagnosticsPanel
                traceId={error.traceId}
                errorId={error.errorId}
                debug={error.debug}
                compact
              />
            </div>
          ) : null}
          {success ? <Alert variant="success">{success}</Alert> : null}
          <label className="text-sm text-slate-200">
            Email
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              autoComplete="email"
              required
            />
            <FieldError message={fieldErrors.email?.[0]} />
          </label>
          <label className="text-sm text-slate-200">
            Reset token
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="text"
              value={token}
              onChange={(event) => setToken(event.target.value)}
              required
            />
            <FieldError message={fieldErrors.token?.[0]} />
          </label>
          <label className="text-sm text-slate-200">
            New password
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="password"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              autoComplete="new-password"
              required
            />
            <FieldError message={fieldErrors.newPassword?.[0]} />
          </label>
          <label className="text-sm text-slate-200">
            Confirm new password
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="password"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              autoComplete="new-password"
              required
            />
            <FieldError message={fieldErrors.confirmPassword?.[0]} />
          </label>
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? "Saving..." : "Update password"}
          </button>
          {success ? (
            <div className="text-xs text-slate-400">
              <Link className="hover:text-slate-200" to="/login">
                Back to sign in
              </Link>
            </div>
          ) : null}
        </form>
      </Card>
    </div>
  );
};

export default ResetPassword;
