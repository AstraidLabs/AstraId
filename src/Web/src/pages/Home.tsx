import { useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";
import Card from "../components/Card";
import Alert from "../components/Alert";
import { getPublicMessage } from "../api/endpoints";

const Home = () => {
  const auth = useAuth();
  const [publicMessage, setPublicMessage] = useState<string>("");
  const [error, setError] = useState<string>("");

  useEffect(() => {
    let mounted = true;

    const load = async () => {
      try {
        const data = await getPublicMessage();
        if (mounted) {
          setPublicMessage(data.message);
        }
      } catch {
        if (mounted) {
          setError("Nepodařilo se načíst veřejné API.");
        }
      }
    };

    void load();

    return () => {
      mounted = false;
    };
  }, []);

  return (
    <div className="flex flex-col gap-6">
      <Card
        title="Vítejte v AstraId"
        description="SPA běží na React + Vite a používá Authorization Code + PKCE."
      >
        <div className="flex flex-col gap-3 text-sm text-slate-300">
          <p>
            Stav autentizace: {" "}
            <span className="font-semibold text-white">
              {auth.isAuthenticated ? "Přihlášen" : "Nepřihlášen"}
            </span>
          </p>
          <p>
            Použijte navigaci pro profil, administraci nebo kontrolu
            integrací.
          </p>
        </div>
      </Card>

      <Card
        title="Veřejné API"
        description="Endpoint /api/public je dostupný bez přihlášení."
      >
        {error ? <Alert variant="warning">{error}</Alert> : null}
        <div className="mt-4 rounded-xl border border-slate-800 bg-slate-950/60 p-4 text-sm text-slate-200">
          {publicMessage || "Načítání..."}
        </div>
      </Card>
    </div>
  );
};

export default Home;
