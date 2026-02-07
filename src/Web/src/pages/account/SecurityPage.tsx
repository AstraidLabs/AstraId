import { Link } from "react-router-dom";
import AccountPageHeader from "../../components/account/AccountPageHeader";

const cardClass =
  "rounded-xl border border-slate-700 bg-slate-950/40 p-4 text-sm text-slate-100 transition hover:border-indigo-400";

export default function SecurityPage() {
  return (
    <div>
      <AccountPageHeader
        title="Security"
        description="Manage core account protections and active sessions from dedicated pages."
      />

      <div className="grid gap-3 md:grid-cols-2">
        <Link to="/account/password" className={cardClass}>
          <p className="font-semibold">Password</p>
          <p className="mt-1 text-slate-400">Update your password and optionally sign out other sessions.</p>
        </Link>

        <Link to="/account/mfa" className={cardClass}>
          <p className="font-semibold">Multi-factor authentication</p>
          <p className="mt-1 text-slate-400">Enable or disable authenticator-based sign-in protection.</p>
        </Link>

        <Link to="/account/sessions" className={cardClass}>
          <p className="font-semibold">Sessions</p>
          <p className="mt-1 text-slate-400">Revoke all other active sessions in one action.</p>
        </Link>

        <Link to="/account/security-events" className={cardClass}>
          <p className="font-semibold">Security events</p>
          <p className="mt-1 text-slate-400">Review sensitive account activity once event timeline is available.</p>
        </Link>
      </div>
    </div>
  );
}
