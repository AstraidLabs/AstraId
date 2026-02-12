import { useLanguage } from "../../i18n/LanguageProvider";
import AccountPageHeader from "../../components/account/AccountPageHeader";
import { useAuthSession } from "../../auth/useAuthSession";

export default function ProfilePage() {
  const { t } = useLanguage();
  const { session } = useAuthSession();

  return (
    <div>
      <AccountPageHeader title={t("account.profile.title")} description={t("account.profile.description")} />
      <div className="rounded-xl border border-slate-800 bg-slate-950/50 p-5 text-sm text-slate-200">
        <div className="grid gap-3 md:grid-cols-2">
          <p><span className="text-slate-400">{t("account.profile.username")}:</span> {session?.userName ?? t("common.unknown")}</p>
          <p><span className="text-slate-400">{t("account.profile.email")}:</span> {session?.email ?? t("common.unknown")}</p>
          <p><span className="text-slate-400">{t("account.profile.userId")}:</span> {session?.userId ?? t("common.unknown")}</p>
          <p><span className="text-slate-400">{t("account.profile.authentication")}:</span> {session?.isAuthenticated ? t("account.profile.active") : t("account.profile.inactive")}</p>
        </div>
      </div>
    </div>
  );
}
