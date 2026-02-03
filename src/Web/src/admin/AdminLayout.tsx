import { Outlet } from "react-router-dom";
import AppShell from "./components/AppShell";
import ToastViewport from "./components/ToastViewport";

export default function AdminLayout() {
  return (
    <>
      <AppShell>
        <Outlet />
      </AppShell>
      <ToastViewport />
    </>
  );
}
