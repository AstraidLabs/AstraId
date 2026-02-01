import { useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";
import { useNavigate } from "react-router-dom";
import Card from "../components/Card";
import Alert from "../components/Alert";

const Callback = () => {
  const auth = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string>("");

  useEffect(() => {
    const handleCallback = async () => {
      try {
        await auth.signinRedirectCallback();
        navigate("/", { replace: true });
      } catch {
        setError("Nepodařilo se dokončit přihlášení.");
      }
    };

    void handleCallback();
  }, [auth, navigate]);

  return (
    <div className="flex flex-col gap-6">
      <Card title="Přihlašování" description="Zpracováváme OIDC callback.">
        {error ? (
          <Alert variant="error">{error}</Alert>
        ) : (
          <Alert variant="info">Zpracováváme přihlášení...</Alert>
        )}
      </Card>
    </div>
  );
};

export default Callback;
