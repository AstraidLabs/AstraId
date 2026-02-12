import { Link } from "react-router-dom";
import Alert from "../components/Alert";
import Card from "../components/Card";
import { useAuthSession } from "../auth/useAuthSession";
import { useLanguage } from "../i18n/LanguageProvider";

const AccountSecurity = () => {
  const { t } = useLanguage();
  const { session } = useAuthSession();
  const isAuthenticated = session?.isAuthenticated ?? false;

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-6">
      <Card title={t("account.security.title")} description={t("account.security.description")}>
        {isAuthenticated ? (
          <div className="space-y-3 text-sm text-slate-300">
            <p>{t("account.security.legacyDescription")}</p>
            <Link className="inline-flex rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400" to="/account/security">
              {t("account.security.openCenter")}
            </Link>
          </div>
        ) : (
          <Alert variant="info">{t("account.security.signInRequired")}</Alert>
        )}
      </Card>
    </div>
  );
};

export default AccountSecurity;
