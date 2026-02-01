import type { PropsWithChildren } from "react";

const variants = {
  info: "border-sky-400/40 bg-sky-500/10 text-sky-100",
  warning: "border-amber-400/40 bg-amber-500/10 text-amber-100",
  error: "border-rose-400/40 bg-rose-500/10 text-rose-100",
  success: "border-emerald-400/40 bg-emerald-500/10 text-emerald-100"
};

type AlertProps = PropsWithChildren<{ variant?: keyof typeof variants }>;

const Alert = ({ variant = "info", children }: AlertProps) => (
  <div className={`rounded-xl border px-4 py-3 text-sm ${variants[variant]}`}>
    {children}
  </div>
);

export default Alert;
