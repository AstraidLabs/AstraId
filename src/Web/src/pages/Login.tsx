import { useState } from "react";
import {
  ArrowRight,
  Eye,
  EyeOff,
  Home,
  Lock,
  Mail,
  ShieldCheck,
  UserPlus
} from "lucide-react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import Alert from "../components/Alert";
import Card from "../components/Card";
import DiagnosticsPanel from "../components/DiagnosticsPanel";
import FieldError from "../components/FieldError";
import { login, resolveReturnUrl } from "../api/authServer";
import { AppError, type FieldErrors } from "../api/errors";
import LanguagePillPopover from "../components/LanguagePillPopover";
import useDocumentMeta from "../hooks/useDocumentMeta";
import { useLanguage } from "../i18n/LanguageProvider";

const Login = () => {
  const { locale, setLocale, t } = useLanguage();

  useDocumentMeta({
    title: t("login.metaTitle"),
    description: t("login.metaDescription")
  });

  const navigate = useNavigate();
  const [params] = useSearchParams();
  const returnUrl = params.get("returnUrl");
  const [emailOrUsername, setEmailOrUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
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
    <main className="mx-auto w-full max-w-md">
      <Card title={t("login.cardTitle")} titleAs="h1" description={t("login.cardDescription")}>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit} noValidate>
          <div className="-mb-1 flex items-center justify-between gap-2">
            <p className="inline-flex items-center gap-1 text-xs uppercase tracking-[0.18em] text-indigo-300">
              <ShieldCheck className="h-3.5 w-3.5" aria-hidden="true" />
              {t("auth.login.badge")}
            </p>
            <Link
              to="/"
              aria-label={t("auth.backToHomeAria")}
              className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900 px-3 py-1.5 text-xs font-medium text-slate-200 transition hover:border-slate-600 hover:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:ring-offset-slate-950"
            >
              <Home className="h-4 w-4" aria-hidden="true" />
              {t("auth.backToHome")}
            </Link>
          </div>

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
            <label htmlFor="login-identity" className="text-sm text-slate-200">
              {t("login.emailOrUsername")}
            </label>
            <div className="relative mt-2">
              <Mail
                className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500"
                aria-hidden="true"
              />
              <input
                id="login-identity"
                className="w-full rounded-lg border border-slate-700 bg-slate-950 py-2 pl-10 pr-3 text-white outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/40"
                type="text"
                value={emailOrUsername}
                onChange={(event) => setEmailOrUsername(event.target.value)}
                autoComplete="username"
                aria-invalid={Boolean(fieldErrors.emailOrUsername?.[0])}
                aria-describedby={fieldErrors.emailOrUsername?.[0] ? "login-identity-error" : undefined}
                required
              />
            </div>
            <div id="login-identity-error">
              <FieldError message={fieldErrors.emailOrUsername?.[0]} />
            </div>
          </div>

          <div>
            <div className="mb-2 flex items-center justify-between gap-2">
              <label htmlFor="login-password" className="text-sm text-slate-200">
                {t("login.password")}
              </label>
              <Link
                className="text-xs text-indigo-300 transition hover:text-indigo-200 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                to="/forgot-password"
              >
                {t("login.forgotPassword")}
              </Link>
            </div>
            <div className="relative">
              <Lock
                className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500"
                aria-hidden="true"
              />
              <input
                id="login-password"
                className="w-full rounded-lg border border-slate-700 bg-slate-950 py-2 pl-10 pr-11 text-white outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/40"
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                autoComplete="current-password"
                aria-invalid={Boolean(fieldErrors.password?.[0])}
                aria-describedby={fieldErrors.password?.[0] ? "login-password-error" : undefined}
                required
              />
              <button
                type="button"
                onClick={() => setShowPassword((prev) => !prev)}
                className="absolute right-2 top-1/2 inline-flex -translate-y-1/2 rounded p-1 text-slate-300 transition hover:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500"
                aria-label={showPassword ? t("login.hidePassword") : t("login.showPassword")}
                aria-pressed={showPassword}
              >
                {showPassword ? <EyeOff className="h-4 w-4" aria-hidden="true" /> : <Eye className="h-4 w-4" aria-hidden="true" />}
              </button>
            </div>
            <div id="login-password-error">
              <FieldError message={fieldErrors.password?.[0]} />
            </div>
          </div>

          <LanguagePillPopover value={locale} onChange={setLocale} />

          <button
            type="submit"
            disabled={isSubmitting}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:ring-offset-slate-950 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <ArrowRight className="h-4 w-4" aria-hidden="true" />
            {isSubmitting ? t("login.submitting") : t("login.submit")}
          </button>

          <Link
            to="/register"
            className="inline-flex items-center justify-center gap-2 rounded-lg border border-slate-600 bg-slate-900 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500 hover:bg-slate-800 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:ring-offset-slate-950"
          >
            <UserPlus className="h-4 w-4" aria-hidden="true" />
            {t("login.createAccount")}
          </Link>
        </form>
      </Card>
    </main>
  );
};

export default Login;
