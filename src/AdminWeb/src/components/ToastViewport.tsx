import { useEffect, useState } from "react";
import { subscribeToToasts, ToastPayload } from "./toast";

type ToastEntry = ToastPayload & { id: string };

const toneClasses: Record<string, string> = {
  error: "border-red-400/40 bg-red-950/80 text-red-100",
  success: "border-emerald-400/40 bg-emerald-950/80 text-emerald-100",
  info: "border-slate-400/40 bg-slate-900/80 text-slate-100",
};

export default function ToastViewport() {
  const [toasts, setToasts] = useState<ToastEntry[]>([]);

  useEffect(() => {
    return subscribeToToasts((payload) => {
      const entry = { ...payload, tone: payload.tone ?? "info", id: crypto.randomUUID() };
      setToasts((current) => [...current, entry]);
      setTimeout(() => {
        setToasts((current) => current.filter((toast) => toast.id !== entry.id));
      }, 4000);
    });
  }, []);

  if (toasts.length === 0) {
    return null;
  }

  return (
    <div className="fixed right-6 top-6 z-50 flex w-full max-w-sm flex-col gap-3">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={`rounded-lg border px-4 py-3 text-sm shadow-lg backdrop-blur ${
            toneClasses[toast.tone ?? "info"]
          }`}
        >
          {toast.message}
        </div>
      ))}
    </div>
  );
}
