import type { PropsWithChildren } from "react";

type CardProps = PropsWithChildren<{
  title?: string;
  description?: string;
  titleAs?: "h1" | "h2" | "h3";
}>;

const Card = ({ title, description, titleAs = "h2", children }: CardProps) => {
  const TitleTag = titleAs;

  return (
    <section className="rounded-2xl border border-slate-800 bg-slate-900/70 p-6 shadow-xl shadow-slate-950/40">
    {title ? (
      <header className="mb-4">
        <TitleTag className="text-lg font-semibold text-slate-100">{title}</TitleTag>
        {description ? (
          <p className="mt-1 text-sm text-slate-400">{description}</p>
        ) : null}
      </header>
    ) : null}
    {children}
    </section>
  );
};

export default Card;
