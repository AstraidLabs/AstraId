import { useEffect, useMemo, useState } from "react";
import { AlertTriangle, CheckCircle2, RefreshCw, ShieldAlert, XCircle } from "lucide-react";
import { AppError, apiRequest } from "../api/http";
import type { AdminPlatformHealthResponse, PlatformHealthCheck } from "../api/types";

type RefreshSeconds = 0 | 10 | 30 | 60;

const statusBadgeClass: Record<string, string> = {
  Healthy: "border-emerald-500/50 bg-emerald-500/10 text-emerald-300",
  Degraded: "border-amber-500/50 bg-amber-500/10 text-amber-300",
  Unhealthy: "border-rose-500/50 bg-rose-500/10 text-rose-300",
};

const getStatusIcon = (status: string) => {
  if (status === "Healthy") {
    return <CheckCircle2 className="h-4 w-4 text-emerald-400" aria-hidden />;
  }
  if (status === "Degraded") {
    return <AlertTriangle className="h-4 w-4 text-amber-400" aria-hidden />;
  }

  return <XCircle className="h-4 w-4 text-rose-400" aria-hidden />;
};

export default function PlatformHealthPage() {
  const [data, setData] = useState<AdminPlatformHealthResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshSeconds, setRefreshSeconds] = useState<RefreshSeconds>(30);
  const [errorState, setErrorState] = useState<"forbidden" | "unavailable" | "network" | null>(null);

  const loadHealth = async () => {
    setLoading(true);
    try {
      const result = await apiRequest<AdminPlatformHealthResponse>("/ops/health", { suppressToast: true });
      setData(result);
      setErrorState(null);
    } catch (error) {
      if (error instanceof AppError) {
        if (error.status === 401 || error.status === 403) {
          setErrorState("forbidden");
        } else if (error.status === 503) {
          setErrorState("unavailable");
        } else {
          setErrorState("network");
        }
      } else {
        setErrorState("network");
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadHealth();
  }, []);

  useEffect(() => {
    if (refreshSeconds === 0) {
      return;
    }

    const timerId = window.setInterval(() => {
      void loadHealth();
    }, refreshSeconds * 1000);

    return () => {
      window.clearInterval(timerId);
    };
  }, [refreshSeconds]);

  const checks = useMemo(() => {
    if (!data) {
      return [];
    }

    return [...data.checks].sort((a, b) => {
      const rank = (status: string) => (status === "Unhealthy" ? 3 : status === "Degraded" ? 2 : 1);
      return rank(b.status) - rank(a.status) || b.durationMs - a.durationMs;
    });
  }, [data]);

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-white">Platform Health</h1>
          <p className="text-sm text-slate-300">Operational health checks exposed by Api /ops/health.</p>
        </div>
        <div className="flex items-center gap-3">
          <label className="text-xs uppercase tracking-wide text-slate-400">Auto refresh</label>
          <select
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
            value={refreshSeconds}
            onChange={(event) => setRefreshSeconds(Number(event.target.value) as RefreshSeconds)}
          >
            <option value={0}>Off</option>
            <option value={10}>10s</option>
            <option value={30}>30s</option>
            <option value={60}>60s</option>
          </select>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <span className={`inline-flex items-center rounded-full border px-3 py-1 text-sm font-semibold ${statusBadgeClass[data?.overallStatus ?? "Degraded"]}`}>
          {getStatusIcon(data?.overallStatus ?? "Degraded")}
          <span className="ml-2">{data?.overallStatus ?? "Unknown"}</span>
        </span>
        <span className="text-sm text-slate-300">
          Last checked: {data ? new Date(data.checkedAtUtc).toLocaleString() : "—"}
        </span>
        <span className="text-sm text-slate-300">Environment: {data?.environment ?? "—"}</span>
      </div>

      {errorState === "forbidden" && (
        <div className="rounded-lg border border-amber-800 bg-amber-950/30 px-4 py-3 text-sm text-amber-200">
          You don’t have access.
        </div>
      )}

      {errorState === "unavailable" && (
        <div className="rounded-lg border border-amber-800 bg-amber-950/30 px-4 py-3 text-sm text-amber-200">
          Checks unavailable. Last known timestamp: {data ? new Date(data.checkedAtUtc).toLocaleString() : "none"}.
        </div>
      )}

      {errorState === "network" && (
        <div className="flex items-center justify-between rounded-lg border border-rose-900 bg-rose-950/20 px-4 py-3 text-sm text-rose-200">
          <span>Network error while loading health checks.</span>
          <button
            className="inline-flex items-center gap-2 rounded-md border border-rose-800 px-3 py-1 text-rose-100 hover:bg-rose-900/40"
            onClick={() => void loadHealth()}
          >
            <RefreshCw className="h-4 w-4" aria-hidden />
            Retry
          </button>
        </div>
      )}

      <div className="grid gap-3 md:grid-cols-3">
        <SummaryTile label="Healthy" value={data?.summary.healthy ?? 0} tone="emerald" />
        <SummaryTile label="Degraded" value={data?.summary.degraded ?? 0} tone="amber" />
        <SummaryTile label="Unhealthy" value={data?.summary.unhealthy ?? 0} tone="rose" />
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-800">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-900 text-slate-300">
            <tr>
              <th className="px-4 py-3 font-medium">Status</th>
              <th className="px-4 py-3 font-medium">Name</th>
              <th className="px-4 py-3 font-medium">Critical</th>
              <th className="px-4 py-3 font-medium">Duration</th>
              <th className="px-4 py-3 font-medium">Last success</th>
              <th className="px-4 py-3 font-medium">Message</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800 bg-slate-950/40">
            {loading && (
              <tr>
                <td colSpan={6} className="px-4 py-6 text-center text-slate-400">Loading checks...</td>
              </tr>
            )}
            {!loading && checks.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-6 text-center text-slate-400">No check data available.</td>
              </tr>
            )}
            {checks.map((check) => (
              <HealthRow key={check.key} check={check} />
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function SummaryTile({ label, value, tone }: { label: string; value: number; tone: "emerald" | "amber" | "rose" }) {
  return (
    <div className={`rounded-lg border bg-slate-900/40 p-4 ${tone === "emerald" ? "border-emerald-800" : tone === "amber" ? "border-amber-800" : "border-rose-800"}`}>
      <div className="text-xs uppercase tracking-wide text-slate-400">{label}</div>
      <div className="mt-1 text-2xl font-semibold text-white">{value}</div>
    </div>
  );
}

function HealthRow({ check }: { check: PlatformHealthCheck }) {
  return (
    <tr className="text-slate-100">
      <td className="px-4 py-3">
        <span className="inline-flex items-center gap-2">{getStatusIcon(check.status)} {check.status}</span>
      </td>
      <td className="px-4 py-3 font-medium text-slate-200">{check.name}</td>
      <td className="px-4 py-3">
        {check.isCritical ? (
          <span className="inline-flex items-center gap-1 rounded-full border border-rose-700/80 bg-rose-900/30 px-2 py-0.5 text-xs text-rose-200">
            <ShieldAlert className="h-3.5 w-3.5" aria-hidden /> Critical
          </span>
        ) : (
          <span className="text-xs text-slate-400">No</span>
        )}
      </td>
      <td className="px-4 py-3 text-slate-300">{check.durationMs} ms</td>
      <td className="px-4 py-3 text-slate-300">{check.lastSuccessUtc ? new Date(check.lastSuccessUtc).toLocaleString() : "Never"}</td>
      <td className="px-4 py-3 text-slate-300">{check.message ?? "—"}</td>
    </tr>
  );
}
