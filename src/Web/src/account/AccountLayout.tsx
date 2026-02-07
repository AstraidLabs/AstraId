import { Outlet } from "react-router-dom";
import AccountSidebar from "./AccountSidebar";

export default function AccountLayout() {
  return (
    <section className="rounded-2xl border border-slate-800 bg-slate-900/30 p-4 md:p-6">
      <div className="grid gap-5 md:grid-cols-[220px_1fr]">
        <aside>
          <AccountSidebar />
        </aside>
        <main>
          <Outlet />
        </main>
      </div>
    </section>
  );
}
