import { useState } from "react";
import Card from "../components/Card";
import Alert from "../components/Alert";
import { forgotPassword } from "../api/authServer";

const ForgotPassword = () => {
  const [email, setEmail] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError("");
    setSuccess("");
    setIsSubmitting(true);

    try {
      await forgotPassword({ email });
      setSuccess(
        "Pokud účet existuje, poslali jsme instrukce pro obnovu hesla."
      );
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Nepodařilo se odeslat žádost."
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-md">
      <Card title="Obnova hesla" description="Zadejte e-mail k obnovení hesla.">
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
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? "Odesíláme..." : "Odeslat odkaz"}
          </button>
        </form>
      </Card>
    </div>
  );
};

export default ForgotPassword;
