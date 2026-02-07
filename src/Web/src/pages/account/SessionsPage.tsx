import { useState } from "react";
import { revokeOtherSessionsAccount } from "../../account/api";
import AccountPageHeader from "../../components/account/AccountPageHeader";
import InlineAlert from "../../components/account/InlineAlert";

export default function SessionsPage() {
  const [working, setWorking] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  const onClick = async () => {
    if (!window.confirm("Sign out all active sessions? You will need to sign in again on other devices.")) return;
    setWorking(true);
    try {
      const result = await revokeOtherSessionsAccount();
      setMessage(result.message);
    } finally {
      setWorking(false);
    }
  };

  return (
    <div className="space-y-3">
      <AccountPageHeader title="Sign out all sessions" description="Invalidate all active sessions except the current flow." />
      {message ? <InlineAlert kind="success" message={message} /> : null}
      <button type="button" onClick={onClick} disabled={working} className="rounded-lg border border-rose-700 px-4 py-2 text-sm font-semibold text-rose-200 hover:border-rose-500 disabled:opacity-60">{working ? "Signing out..." : "Sign out all sessions"}</button>
    </div>
  );
}
