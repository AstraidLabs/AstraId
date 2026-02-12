import Container from "./Container";
import LanguageSelector from "./LanguageSelector";
import { useAuthSession } from "../auth/useAuthSession";
import { isAuthenticatedSession } from "../auth/sessionState";
import { useLanguage } from "../i18n/LanguageProvider";

const GlobalFooter = () => {
  const { t } = useLanguage();
  const { session, status } = useAuthSession();
  const isAuthenticated = isAuthenticatedSession(status, session);

  return (
    <footer className="border-t border-slate-800/80 bg-slate-950/60 py-4">
      <Container>
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <p className="text-xs text-slate-500">
            {t("footer.copyright").replace("{{year}}", String(new Date().getFullYear()))}
          </p>
          <LanguageSelector authenticated={isAuthenticated} compact />
        </div>
      </Container>
    </footer>
  );
};

export default GlobalFooter;
