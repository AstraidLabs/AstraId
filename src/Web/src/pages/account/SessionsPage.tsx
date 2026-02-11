import { useLanguage } from "../../i18n/LanguageProvider";
import { useState } from "react";
import { revokeOtherSessionsAccount } from "../../account/api";
import AccountPageHeader from "../../components/account/AccountPageHeader";
import InlineAlert from "../../components/account/InlineAlert";

export default function SessionsPage() {
  const { t } = useLanguage();
  const [working, setWorking] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  const onClick = async () => {
    if (!window.confirm(t("account.sessions.confirm"))) return;
    setWorking(true);
    try {
      const result = await revokeOtherSessionsAccount();
      setMessage(result.message);
    } finally {
      setWorking(false);
    }
  };

  return (
    <div className="space-y-3">
      <AccountPageHeader title={t("account.sessions.title")} description={t("account.sessions.description")} />
      {message ? <InlineAlert kind="success" message={message} /> : null}
      <button type="button" onClick={onClick} disabled={working} className="rounded-lg border border-rose-700 px-4 py-2 text-sm font-semibold text-rose-200 hover:border-rose-500 disabled:opacity-60">{working ? t("account.sessions.submitting") : t("account.sessions.submit")}</button>
    </div>
  );
}
