import { Navigate, Route, Routes } from "react-router-dom";
import AppShell from "./components/AppShell";
import ToastViewport from "./components/ToastViewport";
import Dashboard from "./pages/Dashboard";
import ClientsList from "./pages/ClientsList";
import ClientCreate from "./pages/ClientCreate";
import ClientEdit from "./pages/ClientEdit";

export default function App() {
  return (
    <>
      <AppShell>
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/clients" element={<ClientsList />} />
          <Route path="/clients/new" element={<ClientCreate />} />
          <Route path="/clients/:id" element={<ClientEdit />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AppShell>
      <ToastViewport />
    </>
  );
}
