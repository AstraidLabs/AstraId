import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import Alert from "../components/Alert";
import Card from "../components/Card";
import { login, resolveReturnUrl } from "../api/authServer";

const Login = () => {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const returnUrl = params.get("returnUrl");
  const [emailOrUsername, setEmailOrUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError("");
    setIsSubmitting(true);

    try {
      await login({
        emailOrUsername,
        password,
        returnUrl
      });

      const resolvedReturnUrl = resolveReturnUrl(returnUrl);
      if (resolvedReturnUrl) {
        window.location.href = resolvedReturnUrl;
      } else {
        navigate("/", { replace: true });
      }
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Nepodařilo se přihlásit."
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-md">
      <Card title="Přihlášení" description="Použijte účet z AuthServeru.">
        <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
          {error ? <Alert variant="error">{error}</Alert> : null}
          <label className="text-sm text-slate-200">
            E-mail nebo uživatelské jméno
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="text"
              value={emailOrUsername}
              onChange={(event) => setEmailOrUsername(event.target.value)}
              autoComplete="username"
              required
            />
          </label>
          <label className="text-sm text-slate-200">
            Heslo
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete="current-password"
              required
            />
          </label>
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? "Přihlašování..." : "Přihlásit"}
          </button>
          <div className="flex flex-wrap gap-3 text-xs text-slate-400">
            <Link className="hover:text-slate-200" to="/forgot-password">
              Zapomenuté heslo
            </Link>
          </div>
        </form>
      </Card>
    </div>
  );
};

export default Login;
