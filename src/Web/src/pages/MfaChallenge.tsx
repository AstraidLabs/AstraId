import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import Alert from "../components/Alert";
import Card from "../components/Card";
import DiagnosticsPanel from "../components/DiagnosticsPanel";
import FieldError from "../components/FieldError";
import { loginMfa, resolveReturnUrl } from "../api/authServer";
import { AppError, type FieldErrors } from "../api/errors";
import { useLanguage } from "../i18n/LanguageProvider";

const MfaChallenge = () => {
  const { t } = useLanguage();
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const token = params.get("token") ?? "";
  const returnUrl = params.get("returnUrl");
  const [code, setCode] = useState("");
  const [useRecoveryCode, setUseRecoveryCode] = useState(false);
  const [rememberMachine, setRememberMachine] = useState(false);
  const [error, setError] = useState<AppError | null>(null);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setFieldErrors({});
    setIsSubmitting(true);

    try {
      const response = await loginMfa({
        mfaToken: token,
        code,
        useRecoveryCode,
        rememberMachine
      });

      const resolved = resolveReturnUrl(
        response.redirectTo ?? returnUrl ?? null
      );
      if (resolved) {
        window.location.href = resolved;
      } else {
        navigate("/", { replace: true });
      }
    } catch (err) {
      if (err && typeof err === "object" && "status" in err) {
        const appError = err as AppError;
        setError(appError);
        setFieldErrors(appError.fieldErrors ?? {});
      } else {
        setError(new AppError({ status: 500, detail: t("common.requestFailed") }));
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-md">
      <Card
        title={t("mfa.title")}
        description={t("mfa.description")}
      >
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
          {!token ? (
            <Alert variant="error">
              {t("mfa.tokenMissing")}
            </Alert>
          ) : null}
          <label className="text-sm text-slate-200">
            {useRecoveryCode ? t("mfa.recoveryCode") : t("mfa.code")}
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="text"
              value={code}
              onChange={(event) => setCode(event.target.value)}
              autoComplete="one-time-code"
              required
            />
            <FieldError message={fieldErrors.code?.[0]} />
          </label>
          <label className="flex items-center gap-3 text-xs text-slate-400">
            <input
              type="checkbox"
              checked={useRecoveryCode}
              onChange={(event) => setUseRecoveryCode(event.target.checked)}
            />
            {t("mfa.useRecovery")}
          </label>
          {!useRecoveryCode ? (
            <label className="flex items-center gap-3 text-xs text-slate-400">
              <input
                type="checkbox"
                checked={rememberMachine}
                onChange={(event) => setRememberMachine(event.target.checked)}
              />
              {t("mfa.remember")}
            </label>
          ) : null}
          <button
            type="submit"
            disabled={isSubmitting || !token}
            className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? t("mfa.submitting") : t("mfa.submit")}
          </button>
          <div className="text-xs text-slate-400">
            <Link className="hover:text-slate-200" to="/login">
              Back to sign in
            </Link>
          </div>
        </form>
      </Card>
    </div>
  );
};

export default MfaChallenge;
