import AccountPageHeader from "../../components/account/AccountPageHeader";
import { useAuthSession } from "../../auth/useAuthSession";

export default function ProfilePage() {
  const { session } = useAuthSession();

  return (
    <div>
      <AccountPageHeader title="Profile" description="Review basic account information from your current session." />
      <div className="rounded-xl border border-slate-800 bg-slate-950/50 p-5 text-sm text-slate-200">
        <div className="grid gap-3 md:grid-cols-2">
          <p><span className="text-slate-400">Username:</span> {session?.userName ?? "Unknown"}</p>
          <p><span className="text-slate-400">Email:</span> {session?.email ?? "Unknown"}</p>
          <p><span className="text-slate-400">User ID:</span> {session?.userId ?? "Unknown"}</p>
          <p><span className="text-slate-400">Authentication:</span> {session?.isAuthenticated ? "Active" : "Inactive"}</p>
        </div>
      </div>
    </div>
  );
}
