import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import Alert from "../components/Alert";
import Card from "../components/Card";
import DiagnosticsPanel from "../components/DiagnosticsPanel";
import FieldError from "../components/FieldError";
import { login, resolveReturnUrl } from "../api/authServer";
import { AppError, type FieldErrors } from "../api/errors";

const Login = () => {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const returnUrl = params.get("returnUrl");
  const [emailOrUsername, setEmailOrUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<AppError | null>(null);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setFieldErrors({});
    setIsSubmitting(true);

    try {
      const response = await login({
        emailOrUsername,
        password,
        returnUrl
      });

      if (response.requiresTwoFactor) {
        const token = response.mfaToken;
        if (!token) {
          throw new Error("Missing MFA token. Please try again.");
        }
        const params = new URLSearchParams();
        params.set("token", token);
        if (response.redirectTo ?? returnUrl) {
          params.set("returnUrl", response.redirectTo ?? returnUrl ?? "");
        }
        navigate(`/mfa?${params.toString()}`, { replace: true });
        return;
      }

      const resolvedReturnUrl = resolveReturnUrl(
        response.redirectTo ?? returnUrl ?? null
      );
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
        setError(new AppError({ status: 500, detail: "Unable to sign in." }));
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-md">
      <Card title="Sign in" description="Use your AuthServer credentials.">
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
          <label className="text-sm text-slate-200">
            Email or username
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="text"
              value={emailOrUsername}
              onChange={(event) => setEmailOrUsername(event.target.value)}
              autoComplete="username"
              required
            />
            <FieldError message={fieldErrors.emailOrUsername?.[0]} />
          </label>
          <label className="text-sm text-slate-200">
            Password
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete="current-password"
              required
            />
            <FieldError message={fieldErrors.password?.[0]} />
          </label>
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? "Signing in..." : "Sign in"}
          </button>
          <div className="flex flex-wrap gap-3 text-xs text-slate-400">
            <Link className="hover:text-slate-200" to="/forgot-password">
              Forgot password
            </Link>
          </div>
        </form>
      </Card>
    </div>
  );
};

export default Login;
