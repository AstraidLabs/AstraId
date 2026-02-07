import PageHeader from "../../components/account/PageHeader";
import { useAuthSession } from "../../auth/useAuthSession";

export default function ProfilePage() {
  const { session } = useAuthSession();

  return (
    <div>
      <PageHeader title="Profile" description="Basic account information from your current session." />
      <div className="space-y-3 rounded-lg border border-slate-800 bg-slate-950/50 p-4 text-sm text-slate-200">
        <p><span className="text-slate-400">User:</span> {session?.userName ?? "Unknown"}</p>
        <p><span className="text-slate-400">Email:</span> {session?.email ?? "Unknown"}</p>
        <p><span className="text-slate-400">User ID:</span> {session?.userId ?? "Unknown"}</p>
        <p><span className="text-slate-400">Authenticated:</span> {session?.isAuthenticated ? "Yes" : "No"}</p>
      </div>
    </div>
  );
}
