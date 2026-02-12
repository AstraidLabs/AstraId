import { useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";
import { useNavigate, useSearchParams } from "react-router-dom";
import Alert from "../components/Alert";
import Card from "../components/Card";
import DiagnosticsPanel from "../components/DiagnosticsPanel";
import { useLanguage } from "../i18n/LanguageProvider";

const Callback = () => {
  const { t } = useLanguage();
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
        setError(err instanceof Error ? err.message : auth.error?.message ?? t("callback.error"));
      }
    };

    void handleCallback();
  }, [auth, navigate, t]);

  return (
    <div className="flex flex-col gap-6">
      <Card title={t("callback.title")} description={t("callback.description")}>
        {error ? (
          <div className="flex flex-col gap-3">
            <Alert variant="error">{error}</Alert>
            <DiagnosticsPanel traceId={traceId} errorId={errorId} compact />
          </div>
        ) : (
          <Alert variant="info">{t("callback.progress")}</Alert>
        )}
      </Card>
    </div>
  );
};

export default Callback;
