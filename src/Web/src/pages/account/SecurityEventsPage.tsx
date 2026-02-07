import { useEffect, useState } from "react";
import AccountPageHeader from "../../components/account/AccountPageHeader";
import { getSecurityEvents, type SecurityEvent } from "../../account/api";

export default function SecurityEventsPage() {
  const [events, setEvents] = useState<SecurityEvent[]>([]);

  useEffect(() => {
    void getSecurityEvents(20).then(setEvents).catch(() => setEvents([]));
  }, []);

  return (
    <div className="space-y-3">
      <AccountPageHeader title="Recent login activity" description="Review recent sign-in events, MFA outcomes, and logout activity." />
      <div className="overflow-x-auto rounded-xl border border-slate-800">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-slate-900/80 text-slate-300"><tr><th className="px-3 py-2">Time (UTC)</th><th className="px-3 py-2">Event</th><th className="px-3 py-2">IP</th><th className="px-3 py-2">User agent</th></tr></thead>
          <tbody>
            {events.map((event) => <tr key={event.id} className="border-t border-slate-800"><td className="px-3 py-2 text-slate-300">{new Date(event.timestampUtc).toLocaleString()}</td><td className="px-3 py-2 text-white">{event.eventType}</td><td className="px-3 py-2 text-slate-300">{event.ipAddress ?? "-"}</td><td className="px-3 py-2 text-slate-400">{(event.userAgent ?? "-").slice(0, 72)}</td></tr>)}
            {events.length === 0 ? <tr><td colSpan={4} className="px-3 py-4 text-slate-400">No recent security events.</td></tr> : null}
          </tbody>
        </table>
      </div>
    </div>
  );
}
