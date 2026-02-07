import { useState } from "react";
import { CheckIcon, EyeIcon, EyeOffIcon, LockIcon, MailIcon, UserPlusIcon } from "../ui/authIcons";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import Alert from "../components/Alert";
import Card from "../components/Card";
import DiagnosticsPanel from "../components/DiagnosticsPanel";
import FieldError from "../components/FieldError";
import { register, resolveReturnUrl } from "../api/authServer";
import { AppError, type FieldErrors } from "../api/errors";

const Register = () => {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const returnUrl = params.get("returnUrl");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [error, setError] = useState<AppError | null>(null);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setFieldErrors({});
    setIsSubmitting(true);

    try {
      await register({
        email,
        password,
        confirmPassword,
        returnUrl
      });

      const resolvedReturnUrl = resolveReturnUrl(returnUrl);
      if (resolvedReturnUrl) {
        window.location.href = resolvedReturnUrl;
      } else {
        navigate("/", { replace: true });
      }
    } catch (err) {
      if (err && typeof err === "object" && "status" in err) {
        const appError = err as AppError;
        setError(appError);
        setFieldErrors(appError.fieldErrors ?? {});
      } else {
        setError(new AppError({ status: 500, detail: "Unable to register." }));
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main className="mx-auto w-full max-w-md">
      <Card title="Create account" description="Set up your AstraId account.">
        <form className="flex flex-col gap-4" onSubmit={handleSubmit} noValidate>
          {error ? (
            <div className="flex flex-col gap-3" role="alert" aria-live="polite">
              <Alert variant="error">{error.detail ?? error.message}</Alert>
              <DiagnosticsPanel
                traceId={error.traceId}
                errorId={error.errorId}
                debug={error.debug}
                compact
              />
            </div>
          ) : null}

          <div>
            <label htmlFor="register-email" className="text-sm text-slate-200">
              Email
            </label>
            <div className="relative mt-2">
              <MailIcon className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" aria-hidden="true" />
              <input
                id="register-email"
                className="w-full rounded-lg border border-slate-700 bg-slate-950 py-2 pl-10 pr-3 text-white outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/40"
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                autoComplete="email"
                aria-invalid={Boolean(fieldErrors.email?.[0])}
                aria-describedby={fieldErrors.email?.[0] ? "register-email-error" : undefined}
                required
              />
            </div>
            <div id="register-email-error">
              <FieldError message={fieldErrors.email?.[0]} />
            </div>
          </div>

          <div>
            <label htmlFor="register-password" className="text-sm text-slate-200">
              Password
            </label>
            <div className="relative mt-2">
              <LockIcon className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" aria-hidden="true" />
              <input
                id="register-password"
                className="w-full rounded-lg border border-slate-700 bg-slate-950 py-2 pl-10 pr-11 text-white outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/40"
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                autoComplete="new-password"
                aria-invalid={Boolean(fieldErrors.password?.[0])}
                aria-describedby={fieldErrors.password?.[0] ? "register-password-error" : "register-password-hint"}
                required
              />
              <button
                type="button"
                onClick={() => setShowPassword((prev) => !prev)}
                className="absolute right-2 top-1/2 inline-flex -translate-y-1/2 rounded p-1 text-slate-300 transition hover:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500"
                aria-label={showPassword ? "Hide password" : "Show password"}
                aria-pressed={showPassword}
              >
                {showPassword ? <EyeOffIcon className="h-4 w-4" aria-hidden="true" /> : <EyeIcon className="h-4 w-4" aria-hidden="true" />}
              </button>
            </div>
            <p id="register-password-hint" className="mt-2 text-xs text-slate-400">
              Use at least 8 characters with a mix of letters and numbers.
            </p>
            <div id="register-password-error">
              <FieldError message={fieldErrors.password?.[0]} />
            </div>
          </div>

          <div>
            <label htmlFor="register-confirm-password" className="text-sm text-slate-200">
              Confirm password
            </label>
            <div className="relative mt-2">
              <CheckIcon className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" aria-hidden="true" />
              <input
                id="register-confirm-password"
                className="w-full rounded-lg border border-slate-700 bg-slate-950 py-2 pl-10 pr-11 text-white outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/40"
                type={showConfirmPassword ? "text" : "password"}
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
                autoComplete="new-password"
                aria-invalid={Boolean(fieldErrors.confirmPassword?.[0])}
                aria-describedby={fieldErrors.confirmPassword?.[0] ? "register-confirm-password-error" : undefined}
                required
              />
              <button
                type="button"
                onClick={() => setShowConfirmPassword((prev) => !prev)}
                className="absolute right-2 top-1/2 inline-flex -translate-y-1/2 rounded p-1 text-slate-300 transition hover:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500"
                aria-label={showConfirmPassword ? "Hide confirmed password" : "Show confirmed password"}
                aria-pressed={showConfirmPassword}
              >
                {showConfirmPassword ? <EyeOffIcon className="h-4 w-4" aria-hidden="true" /> : <EyeIcon className="h-4 w-4" aria-hidden="true" />}
              </button>
            </div>
            <div id="register-confirm-password-error">
              <FieldError message={fieldErrors.confirmPassword?.[0]} />
            </div>
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:ring-offset-slate-950 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <UserPlusIcon className="h-4 w-4" aria-hidden="true" />
            {isSubmitting ? "Creating account..." : "Create account"}
          </button>

          <div className="text-xs text-slate-400">
            Already have an account?{" "}
            <Link className="hover:text-slate-200 focus:outline-none focus:ring-2 focus:ring-indigo-500" to="/login">
              Sign in
            </Link>
          </div>
        </form>
      </Card>
    </main>
  );
};

export default Register;
