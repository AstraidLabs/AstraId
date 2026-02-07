import AccountPageHeader from "../../components/account/AccountPageHeader";

export default function SecurityEventsPage() {
  return (
    <div>
      <AccountPageHeader
        title="Security events"
        description="Track important account activity. This timeline is planned for an upcoming release."
      />
      <div className="rounded-xl border border-dashed border-slate-700 bg-slate-950/30 p-5 text-sm text-slate-300">
        <p className="font-semibold text-white">Coming soon</p>
        <p className="mt-2">You will be able to review login attempts, MFA changes, and sensitive account updates here.</p>
      </div>
    </div>
  );
}
