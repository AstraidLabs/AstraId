import { useLanguage } from "../i18n/LanguageProvider";
import type { DiagnosticsDebug } from "../api/errors";

type DiagnosticsPanelProps = {
  traceId?: string;
  errorId?: string;
  debug?: DiagnosticsDebug;
  compact?: boolean;
};

export default function DiagnosticsPanel({
  traceId,
  errorId,
  debug,
  compact = false
}: DiagnosticsPanelProps) {
  const { t } = useLanguage();
  if (!traceId && !errorId && !debug) {
    return null;
  }

  return (
    <details
      className={`rounded-lg border border-slate-700/60 bg-slate-950/50 ${
        compact ? "px-3 py-2 text-xs" : "px-4 py-3 text-sm"
      }`}
    >
      <summary className="cursor-pointer font-semibold text-slate-200">
        {t("diagnostics.title")}
      </summary>
      <div className="mt-3 space-y-2 text-slate-300">
        {errorId && (
          <div>
            <span className="font-semibold text-slate-200">{t("diagnostics.errorId")}:</span>{" "}
            {errorId}
          </div>
        )}
        {traceId && (
          <div>
            <span className="font-semibold text-slate-200">{t("diagnostics.traceId")}:</span>{" "}
            {traceId}
          </div>
        )}
        {debug?.exceptionType && (
          <div>
            <span className="font-semibold text-slate-200">{t("diagnostics.exception")}:</span>{" "}
            {debug.exceptionType}
          </div>
        )}
        {(debug?.path || debug?.method) && (
          <div>
            <span className="font-semibold text-slate-200">{t("diagnostics.request")}:</span>{" "}
            {[debug.method, debug.path].filter(Boolean).join(" ")}
          </div>
        )}
        {debug?.innerExceptionSummary && (
          <div>
            <span className="font-semibold text-slate-200">
              {t("diagnostics.innerException")}:
            </span>{" "}
            {debug.innerExceptionSummary}
          </div>
        )}
        {debug?.stackTrace && (
          <pre className="max-h-64 overflow-auto rounded-md bg-slate-900/70 p-3 text-xs text-slate-200">
            {debug.stackTrace}
          </pre>
        )}
      </div>
    </details>
  );
}
