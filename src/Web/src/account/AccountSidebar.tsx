import { useLanguage } from "../i18n/LanguageProvider";
import AccountNavItem from "../components/account/AccountNavItem";
import { accountIcons } from "../ui/accountIcons";

export default function AccountSidebar() {
  const { t } = useLanguage();
  const ProfileIcon = accountIcons.profile;
  const SecurityIcon = accountIcons.security;
  const PrivacyIcon = accountIcons.activity;

  return (
    <nav className="space-y-1">
      <p className="px-3 pb-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">{t("account.sidebarTitle")}</p>
      <AccountNavItem to="/account" label={t("account.nav.overview")} icon={<ProfileIcon className="h-[17px] w-[17px]" />} end />
      <AccountNavItem to="/account/security" label={t("account.nav.security")} icon={<SecurityIcon className="h-[17px] w-[17px]" />} />
      <AccountNavItem to="/account/privacy" label={t("account.nav.privacy")} icon={<PrivacyIcon className="h-[17px] w-[17px]" />} />
    </nav>
  );
}
