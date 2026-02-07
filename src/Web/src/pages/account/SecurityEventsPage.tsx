import PageHeader from "../../components/account/PageHeader";

export default function SecurityEventsPage() {
  return (
    <div>
      <PageHeader title="Security events" description="Recent security activity will be available here in a future release." />
      <div className="rounded-lg border border-dashed border-slate-700 bg-slate-950/30 p-5 text-sm text-slate-400">
        No events to display yet.
      </div>
    </div>
  );
}
