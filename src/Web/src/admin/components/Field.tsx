import type { ReactNode } from "react";
import type { DiagnosticsInfo } from "../../api/errors";
import DiagnosticsPanel from "../../components/DiagnosticsPanel";

type FieldProps = {
  label: string;
  hint?: string;
  tooltip?: string;
  error?: string | null;
  required?: boolean;
  children: ReactNode;
};

export function Field({ label, hint, tooltip, error, required, children }: FieldProps) {
  return (
    <div className="flex flex-col gap-2 text-sm text-slate-200">
      <div className="flex items-center gap-2">
        <span className="font-medium text-slate-200">
          {label}
          {required && <span className="ml-1 text-rose-300">*</span>}
        </span>
        {tooltip && <HelpIcon tooltip={tooltip} />}
      </div>
      {children}
      {hint && <HintText>{hint}</HintText>}
      <InlineError message={error} />
    </div>
  );
}

type HelpIconProps = {
  tooltip: string;
};

export function HelpIcon({ tooltip }: HelpIconProps) {
  return (
    <span className="group relative inline-flex">
      <button
        type="button"
        className="flex h-5 w-5 items-center justify-center rounded-full border border-slate-600 text-xs text-slate-300 hover:border-slate-400 hover:text-slate-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-400"
        aria-label="Help"
      >
        ?
      </button>
      <span className="pointer-events-none absolute left-1/2 top-full z-10 mt-2 w-64 -translate-x-1/2 rounded-md border border-slate-700 bg-slate-950 p-2 text-xs text-slate-200 opacity-0 shadow-lg transition-opacity group-hover:opacity-100 group-focus-within:opacity-100">
        {tooltip}
      </span>
    </span>
  );
}

type InlineErrorProps = {
  message?: string | null;
};

export function InlineError({ message }: InlineErrorProps) {
  if (!message) {
    return null;
  }

  return <p className="text-xs text-rose-300">{message}</p>;
}

type HintTextProps = {
  children: ReactNode;
};

export function HintText({ children }: HintTextProps) {
  return <p className="text-xs text-slate-400">{children}</p>;
}

type FormErrorProps = {
  message?: string | null;
  diagnostics?: DiagnosticsInfo;
};

export function FormError({ message, diagnostics }: FormErrorProps) {
  if (!message) {
    return null;
  }

  return (
    <div className="flex flex-col gap-3 rounded-lg border border-rose-500/40 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">
      <span>{message}</span>
      {diagnostics ? (
        <DiagnosticsPanel
          traceId={diagnostics.traceId}
          errorId={diagnostics.errorId}
          debug={diagnostics.debug}
        />
      ) : null}
    </div>
  );
}
