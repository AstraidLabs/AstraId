import { useState } from "react";
import AccountPageHeader from "../../components/account/AccountPageHeader";
import { cancelAccountDeletion, exportMyData, requestAccountDeletion } from "../../account/api";
import { useLanguage } from "../../i18n/LanguageProvider";

export default function PrivacyPage() {
  const { t } = useLanguage();
  const [status, setStatus] = useState<string>("");

  const onExport = async () => {
    const response = await exportMyData();
    if (!response.ok) {
      setStatus(t("account.privacy.exportFailed"));
      return;
    }

    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "astraid-export.json".replace(" ", "");
    a.click();
    URL.revokeObjectURL(url);
    setStatus(t("account.privacy.exportDownloaded"));
  };

  const onRequestDeletion = async () => {
    const response = await requestAccountDeletion({});
    setStatus(t("account.privacy.deletionRequested").replace("{{time}}", new Date(response.cooldownUntilUtc).toLocaleString()));
  };

  const onCancelDeletion = async () => {
    await cancelAccountDeletion();
    setStatus(t("account.privacy.deletionCancelled"));
  };

  return (
    <div className="space-y-4">
      <AccountPageHeader title={t("account.privacy.title")} description={t("account.privacy.description")} />
      <div className="rounded-xl border border-slate-800 p-4">
        <h3 className="font-semibold text-white">{t("account.privacy.exportTitle")}</h3>
        <p className="mt-1 text-sm text-slate-400">{t("account.privacy.exportHelp")}</p>
        <button onClick={() => void onExport()} className="mt-3 rounded-md bg-indigo-500 px-3 py-2 text-sm font-medium text-white">{t("account.privacy.exportButton")}</button>
      </div>
      <div className="rounded-xl border border-slate-800 p-4">
        <h3 className="font-semibold text-white">{t("account.privacy.deleteTitle")}</h3>
        <p className="mt-1 text-sm text-slate-400">{t("account.privacy.deleteHelp")}</p>
        <div className="mt-3 flex gap-2">
          <button onClick={() => void onRequestDeletion()} className="rounded-md bg-rose-500 px-3 py-2 text-sm font-medium text-white">{t("account.privacy.requestDelete")}</button>
          <button onClick={() => void onCancelDeletion()} className="rounded-md border border-slate-700 px-3 py-2 text-sm font-medium text-slate-200">{t("account.privacy.cancelDelete")}</button>
        </div>
      </div>
      {status ? <p className="text-sm text-slate-300">{status}</p> : null}
    </div>
  );
}
