import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { AppError, apiRequest } from "../api/http";
import type { AdminErrorLogDetail } from "../api/types";
import { FormError } from "../components/Field";
import DiagnosticsPanel from "../../components/DiagnosticsPanel";
import { toAdminRoute } from "../../routing";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

export default function DiagnosticsErrorDetail() {
  const { id } = useParams<{ id: string }>();
  const [entry, setEntry] = useState<AdminErrorLogDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [formError, setFormError] = useState<string | null>(null);
  const [formDiagnostics, setFormDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!id) {
      return;
    }
    let isMounted = true;
    const fetchDetail = async () => {
      setLoading(true);
      try {
        const data = await apiRequest<AdminErrorLogDetail>(
          `/admin/api/diagnostics/errors/${id}`,
          { suppressToast: true }
        );
        if (isMounted) {
          setEntry(data);
          setFormError(null);
          setFormDiagnostics(undefined);
        }
      } catch (error) {
        if (error instanceof AppError) {
          const parsed = parseProblemDetailsErrors(error);
          if (isMounted) {
            setFormError(parsed.generalError ?? "Unable to load error details.");
            setFormDiagnostics(parsed.diagnostics);
          }
        } else if (isMounted) {
          setFormError("Unable to load error details.");
          setFormDiagnostics(undefined);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };
    fetchDetail();
    return () => {
      isMounted = false;
    };
  }, [id]);

  if (!id) {
    return null;
  }

  if (loading) {
    return (
      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6 text-slate-300">
        Loading error log...
      </div>
    );
  }

  if (!entry) {
    return (
      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6 text-slate-300">
        Error log not found.
      </div>
    );
  }

  return (
    <section className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-white">Error details</h1>
          <p className="text-sm text-slate-300">
            {entry.method} {entry.path} · {entry.statusCode}
          </p>
        </div>
        <Link
          className="text-sm font-semibold text-indigo-300 hover:text-indigo-200"
          to={toAdminRoute("/diagnostics/errors")}
        >
          Back to list
        </Link>
      </div>

      <FormError message={formError} diagnostics={formDiagnostics} />

      <div className="grid gap-4 rounded-lg border border-slate-800 bg-slate-900/40 p-6 text-sm text-slate-200">
        <div className="grid gap-4 md:grid-cols-2">
          <div>
            <div className="text-xs uppercase tracking-wide text-slate-400">Timestamp</div>
            <div className="mt-1 text-slate-100">
              {new Date(entry.timestampUtc).toLocaleString()}
            </div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-slate-400">Actor</div>
            <div className="mt-1 text-slate-100">
              {entry.actorEmail ?? entry.actorUserId ?? "System"}
            </div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-slate-400">Trace ID</div>
            <div className="mt-1 text-slate-100">{entry.traceId}</div>
          </div>
          <div>
            <div className="text-xs uppercase tracking-wide text-slate-400">Error ID</div>
            <div className="mt-1 text-slate-100">{entry.id}</div>
          </div>
        </div>

        <div>
          <div className="text-xs uppercase tracking-wide text-slate-400">Summary</div>
          <div className="mt-2 rounded-md border border-slate-800 bg-slate-950/60 p-3 text-slate-100">
            <div className="font-semibold">{entry.title}</div>
            <div className="mt-1 text-slate-300">{entry.detail}</div>
          </div>
        </div>

        <DiagnosticsPanel
          traceId={entry.traceId}
          errorId={entry.id}
          debug={{
            exceptionType: entry.exceptionType ?? undefined,
            stackTrace: entry.stackTrace ?? undefined,
            innerExceptionSummary: entry.innerException ?? undefined,
            path: entry.path,
            method: entry.method
          }}
        />
        {entry.stackTrace ? (
          <div>
            <button
              type="button"
              className="rounded-md border border-slate-700 px-3 py-2 text-xs font-semibold text-slate-200 hover:bg-slate-900"
              onClick={async () => {
                await navigator.clipboard.writeText(entry.stackTrace ?? "");
                setCopied(true);
                setTimeout(() => setCopied(false), 2000);
              }}
            >
              {copied ? "Copied" : "Copy stack trace"}
            </button>
          </div>
        ) : null}

        {entry.userAgent && (
          <div>
            <div className="text-xs uppercase tracking-wide text-slate-400">User agent</div>
            <div className="mt-1 text-slate-100">{entry.userAgent}</div>
          </div>
        )}

        {entry.remoteIp && (
          <div>
            <div className="text-xs uppercase tracking-wide text-slate-400">Remote IP</div>
            <div className="mt-1 text-slate-100">{entry.remoteIp}</div>
          </div>
        )}

        {entry.dataJson && (
          <div>
            <div className="text-xs uppercase tracking-wide text-slate-400">Data</div>
            <pre className="mt-2 max-h-64 overflow-auto rounded-md bg-slate-950/60 p-3 text-xs text-slate-200">
              {entry.dataJson}
            </pre>
          </div>
        )}
      </div>
    </section>
  );
}
