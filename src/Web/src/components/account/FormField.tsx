import type { InputHTMLAttributes } from "react";

type Props = {
  label: string;
  error?: string;
} & InputHTMLAttributes<HTMLInputElement>;

export default function FormField({ label, error, className, ...props }: Props) {
  return (
    <label className="text-sm text-slate-200">
      {label}
      <input
        {...props}
        className={`mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white ${className ?? ""}`}
      />
      {error ? <p className="mt-1 text-xs text-rose-300">{error}</p> : null}
    </label>
  );
}
