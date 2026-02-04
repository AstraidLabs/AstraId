import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import Alert from "../components/Alert";
import Card from "../components/Card";
import { loginMfa, resolveReturnUrl } from "../api/authServer";

const MfaChallenge = () => {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const token = params.get("token") ?? "";
  const returnUrl = params.get("returnUrl");
  const [code, setCode] = useState("");
  const [useRecoveryCode, setUseRecoveryCode] = useState(false);
  const [rememberMachine, setRememberMachine] = useState(false);
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError("");
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
      setError(
        err instanceof Error
          ? err.message
          : "Nepodařilo se ověřit MFA kód."
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-md">
      <Card
        title="Ověření MFA"
        description="Zadejte kód z authenticator aplikace nebo použijte recovery code."
      >
        <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
          {error ? <Alert variant="error">{error}</Alert> : null}
          {!token ? (
            <Alert variant="error">
              MFA výzva není dostupná. Přihlaste se prosím znovu.
            </Alert>
          ) : null}
          <label className="text-sm text-slate-200">
            {useRecoveryCode ? "Recovery code" : "Ověřovací kód"}
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="text"
              value={code}
              onChange={(event) => setCode(event.target.value)}
              autoComplete="one-time-code"
              required
            />
          </label>
          <label className="flex items-center gap-3 text-xs text-slate-400">
            <input
              type="checkbox"
              checked={useRecoveryCode}
              onChange={(event) => setUseRecoveryCode(event.target.checked)}
            />
            Použít recovery code
          </label>
          {!useRecoveryCode ? (
            <label className="flex items-center gap-3 text-xs text-slate-400">
              <input
                type="checkbox"
                checked={rememberMachine}
                onChange={(event) => setRememberMachine(event.target.checked)}
              />
              Důvěřovat tomuto zařízení
            </label>
          ) : null}
          <button
            type="submit"
            disabled={isSubmitting || !token}
            className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? "Ověřuji..." : "Pokračovat"}
          </button>
          <div className="text-xs text-slate-400">
            <Link className="hover:text-slate-200" to="/login">
              Zpět na přihlášení
            </Link>
          </div>
        </form>
      </Card>
    </div>
  );
};

export default MfaChallenge;
