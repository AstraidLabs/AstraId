import { useState } from "react";
import { Outlet } from "react-router-dom";
import AccountSidebar from "./AccountSidebar";

export default function AccountLayout() {
  const [open, setOpen] = useState(false);

  return (
    <div className="grid gap-5 md:grid-cols-[280px,minmax(0,1fr)]">
      <aside className="h-fit rounded-2xl border border-slate-800 bg-slate-900/50 p-3">
        <button
          type="button"
          className="mb-3 w-full rounded-xl border border-slate-700 px-3 py-2 text-left text-sm text-slate-200 md:hidden"
          onClick={() => setOpen((current) => !current)}
        >
          {open ? "Close menu" : "Account menu"}
        </button>
        <div className={`${open ? "block" : "hidden"} md:block`}>
          <AccountSidebar onNavigate={() => setOpen(false)} />
        </div>
      </aside>
      <section className="rounded-2xl border border-slate-800 bg-slate-900/30 p-5 md:p-7">
        <Outlet />
      </section>
    </div>
  );
}
