import type { ReactNode } from "react";

type FieldProps = {
  label: string;
  hint?: string;
  tooltip?: string;
  error?: string | null;
  children: ReactNode;
};

export function Field({ label, hint, tooltip, error, children }: FieldProps) {
  return (
    <div className="flex flex-col gap-2 text-sm text-slate-200">
      <div className="flex items-center gap-2">
        <span className="font-medium text-slate-200">{label}</span>
        {tooltip && <HelpIcon tooltip={tooltip} />}
      </div>
      {children}
      {hint && <p className="text-xs text-slate-400">{hint}</p>}
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
