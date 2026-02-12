import { Link } from "react-router-dom";
import Card from "../components/Card";
import { getAdminEntryUrl, isAbsoluteUrl } from "../utils/adminEntry";
import { useLanguage } from "../i18n/LanguageProvider";

const Admin = () => {
  const { t } = useLanguage();
  const adminUrl = getAdminEntryUrl();
  const adminIsExternal = isAbsoluteUrl(adminUrl);

  return (
    <Card title={t("admin.page.title")} description={t("admin.page.description")}>
      <div className="flex flex-col gap-4 text-sm text-slate-300">
        <p>{t("admin.page.location")}</p>
        <p>
          <strong className="text-white">{adminUrl}</strong>
        </p>
        <p>{t("admin.page.hint")}</p>
        <div>
          {adminIsExternal ? (
            <a className="inline-flex rounded-lg bg-slate-800 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:bg-slate-700" href={adminUrl}>
              {t("admin.page.open")}
            </a>
          ) : (
            <Link className="inline-flex rounded-lg bg-slate-800 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:bg-slate-700" to={adminUrl}>
              {t("admin.page.open")}
            </Link>
          )}
        </div>
      </div>
    </Card>
  );
};

export default Admin;
