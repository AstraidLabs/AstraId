import { useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";
import { useNavigate, useSearchParams } from "react-router-dom";
import Alert from "../components/Alert";
import Card from "../components/Card";
import DiagnosticsPanel from "../components/DiagnosticsPanel";

const Callback = () => {
  const auth = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string>("");
  const [params] = useSearchParams();
  const traceId = params.get("traceId") ?? undefined;
  const errorId = params.get("errorId") ?? undefined;

  useEffect(() => {
    const handleCallback = async () => {
      try {
        await auth.signinRedirectCallback();
        navigate("/", { replace: true });
      } catch (err) {
        const message =
          err instanceof Error
            ? err.message
            : auth.error?.message ?? "We couldn’t complete the sign-in.";
        setError(message);
      }
    };

    void handleCallback();
  }, [auth, navigate]);

  return (
    <div className="flex flex-col gap-6">
      <Card title="Signing you in" description="Processing the OIDC callback.">
        {error ? (
          <div className="flex flex-col gap-3">
            <Alert variant="error">{error}</Alert>
            <DiagnosticsPanel traceId={traceId} errorId={errorId} compact />
          </div>
        ) : (
          <Alert variant="info">Completing sign-in...</Alert>
        )}
      </Card>
    </div>
  );
};

export default Callback;
