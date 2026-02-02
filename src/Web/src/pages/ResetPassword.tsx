import { useMemo, useState } from "react";
import { useSearchParams, Link } from "react-router-dom";
import Card from "../components/Card";
import Alert from "../components/Alert";
import { resetPassword } from "../api/authServer";

const ResetPassword = () => {
  const [params] = useSearchParams();
  const initialEmail = useMemo(() => params.get("email") ?? "", [params]);
  const initialToken = useMemo(() => params.get("token") ?? "", [params]);

  const [email, setEmail] = useState(initialEmail);
  const [token, setToken] = useState(initialToken);
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError("");
    setSuccess("");
    setIsSubmitting(true);

    try {
      await resetPassword({
        email,
        token,
        newPassword,
        confirmPassword
      });
      setSuccess("Heslo bylo úspěšně změněno. Přihlaste se znovu.");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Nepodařilo se obnovit heslo."
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-md">
      <Card title="Nastavení nového hesla">
        <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
          {error ? <Alert variant="error">{error}</Alert> : null}
          {success ? <Alert variant="success">{success}</Alert> : null}
          <label className="text-sm text-slate-200">
            E-mail
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              autoComplete="email"
              required
            />
          </label>
          <label className="text-sm text-slate-200">
            Token
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="text"
              value={token}
              onChange={(event) => setToken(event.target.value)}
              required
            />
          </label>
          <label className="text-sm text-slate-200">
            Nové heslo
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="password"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              autoComplete="new-password"
              required
            />
          </label>
          <label className="text-sm text-slate-200">
            Potvrzení nového hesla
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="password"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              autoComplete="new-password"
              required
            />
          </label>
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? "Ukládáme..." : "Změnit heslo"}
          </button>
          {success ? (
            <div className="text-xs text-slate-400">
              <Link className="hover:text-slate-200" to="/login">
                Přejít na přihlášení
              </Link>
            </div>
          ) : null}
        </form>
      </Card>
    </div>
  );
};

export default ResetPassword;
