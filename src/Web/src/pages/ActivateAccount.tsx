import { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import Card from "../components/Card";
import Alert from "../components/Alert";
import { activateAccount } from "../api/authServer";

const ActivateAccount = () => {
  const [params] = useSearchParams();
  const initialEmail = useMemo(() => params.get("email") ?? "", [params]);
  const initialToken = useMemo(() => params.get("token") ?? "", [params]);

  const [email, setEmail] = useState(initialEmail);
  const [token, setToken] = useState(initialToken);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleActivate = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError("");
    setSuccess("");
    setIsSubmitting(true);

    try {
      await activateAccount({ email, token });
      setSuccess("Účet byl aktivován. Přihlaste se.");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Nepodařilo se aktivovat účet."
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-6">
      <Card title="Aktivace účtu">
        <form className="flex flex-col gap-4" onSubmit={handleActivate}>
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
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? "Aktivujeme..." : "Aktivovat účet"}
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

export default ActivateAccount;
