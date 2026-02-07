import { Outlet } from "react-router-dom";

export default function AccountLayout() {
  return (
    <section className="rounded-2xl border border-slate-800 bg-slate-900/30 p-5 md:p-7">
      <Outlet />
    </section>
  );
}
