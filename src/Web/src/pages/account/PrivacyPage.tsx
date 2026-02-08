import { useState } from "react";
import AccountPageHeader from "../../components/account/AccountPageHeader";
import { cancelAccountDeletion, exportMyData, requestAccountDeletion } from "../../account/api";

export default function PrivacyPage() {
  const [status, setStatus] = useState<string>("");

  const onExport = async () => {
    const response = await exportMyData();
    if (!response.ok) {
      setStatus("Failed to export your data.");
      return;
    }

    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "astraid-export.json".replace(" ", "");
    a.click();
    URL.revokeObjectURL(url);
    setStatus("Data export downloaded.");
  };

  const onRequestDeletion = async () => {
    const response = await requestAccountDeletion({});
    setStatus(`Deletion requested. Cooldown until ${new Date(response.cooldownUntilUtc).toLocaleString()}.`);
  };

  const onCancelDeletion = async () => {
    await cancelAccountDeletion();
    setStatus("Deletion request cancelled.");
  };

  return (
    <div className="space-y-4">
      <AccountPageHeader title="Privacy" description="Export your data and manage deletion requests." />
      <div className="rounded-xl border border-slate-800 p-4">
        <h3 className="font-semibold text-white">Export my data</h3>
        <p className="mt-1 text-sm text-slate-400">Download a machine-readable JSON export of your account data.</p>
        <button onClick={() => void onExport()} className="mt-3 rounded-md bg-indigo-500 px-3 py-2 text-sm font-medium text-white">Export my data</button>
      </div>
      <div className="rounded-xl border border-slate-800 p-4">
        <h3 className="font-semibold text-white">Account deletion</h3>
        <p className="mt-1 text-sm text-slate-400">Request deletion of your account after a cooldown period.</p>
        <div className="mt-3 flex gap-2">
          <button onClick={() => void onRequestDeletion()} className="rounded-md bg-rose-500 px-3 py-2 text-sm font-medium text-white">Request deletion</button>
          <button onClick={() => void onCancelDeletion()} className="rounded-md border border-slate-700 px-3 py-2 text-sm font-medium text-slate-200">Cancel deletion</button>
        </div>
      </div>
      {status ? <p className="text-sm text-slate-300">{status}</p> : null}
    </div>
  );
}
