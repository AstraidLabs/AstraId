import { useLocation } from "react-router-dom";
import Card from "../components/Card";
import DiagnosticsPanel from "../components/DiagnosticsPanel";

type ErrorPageProps = {
  title: string;
  description: string;
  hint?: string;
};

export default function ErrorPage({ title, description, hint }: ErrorPageProps) {
  const location = useLocation();
  const params = new URLSearchParams(location.search);
  const traceId = params.get("traceId") ?? undefined;
  const errorId = params.get("errorId") ?? undefined;

  return (
    <Card title={title} description={description}>
      <div className="flex flex-col gap-4 text-sm text-slate-300">
        {hint ? <p>{hint}</p> : null}
        <DiagnosticsPanel traceId={traceId} errorId={errorId} compact />
      </div>
    </Card>
  );
}
