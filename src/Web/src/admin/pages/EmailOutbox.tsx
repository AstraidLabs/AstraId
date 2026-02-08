import { useEffect, useState } from "react";
import { apiRequest } from "../api/http";
import type { AdminEmailOutboxMessage, PagedResult } from "../api/types";
import { pushToast } from "../components/toast";

export default function EmailOutbox() {
  const [result, setResult] = useState<PagedResult<AdminEmailOutboxMessage>>({ items: [], totalCount: 0, page: 1, pageSize: 25 });

  const load = async () => {
    const data = await apiRequest<PagedResult<AdminEmailOutboxMessage>>("/admin/api/diagnostics/email-outbox?page=1&pageSize=50");
    setResult(data);
  };

  useEffect(() => { load(); }, []);

  const retry = async (id: string) => {
    await apiRequest<void>(`/admin/api/diagnostics/email-outbox/${id}/retry`, { method: "POST" });
    pushToast({ tone: "success", message: "Message queued for retry." });
    await load();
  };

  const cancel = async (id: string) => {
    await apiRequest<void>(`/admin/api/diagnostics/email-outbox/${id}/cancel`, { method: "POST" });
    pushToast({ tone: "success", message: "Message cancelled." });
    await load();
  };

  return <div className="flex flex-col gap-4"><h1 className="text-2xl font-semibold text-white">Email Outbox</h1>
    <div className="rounded border border-slate-800 p-4 text-sm text-slate-300">Total: {result.totalCount}</div>
    <div className="overflow-auto rounded border border-slate-800"><table className="min-w-full text-sm"><thead><tr className="text-left text-slate-400"><th className="px-3 py-2">Created</th><th className="px-3 py-2">Type</th><th className="px-3 py-2">To</th><th className="px-3 py-2">Status</th><th className="px-3 py-2">Attempts</th><th className="px-3 py-2">Actions</th></tr></thead><tbody>{result.items.map((item) => <tr key={item.id} className="border-t border-slate-900"><td className="px-3 py-2">{new Date(item.createdUtc).toLocaleString()}</td><td className="px-3 py-2">{item.type}</td><td className="px-3 py-2">{item.toEmail}</td><td className="px-3 py-2">{item.status}</td><td className="px-3 py-2">{item.attempts}/{item.maxAttempts}</td><td className="px-3 py-2 flex gap-2"><button className="rounded border border-slate-700 px-2 py-1" onClick={() => retry(item.id)}>Retry</button><button className="rounded border border-rose-500/40 px-2 py-1 text-rose-200" onClick={() => cancel(item.id)}>Cancel</button></td></tr>)}</tbody></table></div>
  </div>;
}
