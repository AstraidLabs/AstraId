import type { PropsWithChildren } from "react";

type CardProps = PropsWithChildren<{ title?: string; description?: string }>;

const Card = ({ title, description, children }: CardProps) => (
  <section className="rounded-2xl border border-slate-800 bg-slate-900/70 p-6 shadow-xl shadow-slate-950/40">
    {title ? (
      <header className="mb-4">
        <h2 className="text-lg font-semibold text-slate-100">{title}</h2>
        {description ? (
          <p className="mt-1 text-sm text-slate-400">{description}</p>
        ) : null}
      </header>
    ) : null}
    {children}
  </section>
);

export default Card;
