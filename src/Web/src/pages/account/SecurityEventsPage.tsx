import { useEffect, useState } from "react";
import AccountPageHeader from "../../components/account/AccountPageHeader";
import { getLoginHistory, type LoginHistoryEntry } from "../../account/api";
import { useLanguage } from "../../i18n/LanguageProvider";

export default function SecurityEventsPage() {
  const { t } = useLanguage();
  const [events, setEvents] = useState<LoginHistoryEntry[]>([]);

  useEffect(() => {
    void getLoginHistory(30).then(setEvents).catch(() => setEvents([]));
  }, []);

  return (
    <div className="space-y-3">
      <AccountPageHeader title={t("account.events.title")} description={t("account.events.description")} />
      <div className="overflow-x-auto rounded-xl border border-slate-800">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-slate-900/80 text-slate-300"><tr><th className="px-3 py-2">{t("account.events.time")}</th><th className="px-3 py-2">{t("account.events.result")}</th><th className="px-3 py-2">{t("account.events.client")}</th><th className="px-3 py-2">{t("account.events.ip")}</th><th className="px-3 py-2">{t("account.events.userAgent")}</th></tr></thead>
          <tbody>
            {events.map((event) => <tr key={event.id} className="border-t border-slate-800"><td className="px-3 py-2 text-slate-300">{new Date(event.timestampUtc).toLocaleString()}</td><td className="px-3 py-2 text-white">{event.success ? t("account.events.success") : `${t("account.events.failed")} (${event.failureReasonCode ?? "unknown"})`}</td><td className="px-3 py-2 text-slate-300">{event.clientId ?? "-"}</td><td className="px-3 py-2 text-slate-300">{event.ip ?? "-"}</td><td className="px-3 py-2 text-slate-400">{(event.userAgent ?? "-").slice(0, 72)}</td></tr>)}
            {events.length === 0 ? <tr><td colSpan={5} className="px-3 py-4 text-slate-400">{t("account.events.empty")}</td></tr> : null}
          </tbody>
        </table>
      </div>
    </div>
  );
}
