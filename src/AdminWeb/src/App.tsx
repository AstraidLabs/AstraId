import { Navigate, Route, Routes } from "react-router-dom";
import AppShell from "./components/AppShell";
import ToastViewport from "./components/ToastViewport";
import Dashboard from "./pages/Dashboard";
import ClientsList from "./pages/ClientsList";
import ClientCreate from "./pages/ClientCreate";
import ClientEdit from "./pages/ClientEdit";
import ScopesList from "./pages/ScopesList";
import UsersList from "./pages/UsersList";
import AuditList from "./pages/AuditList";

export default function App() {
  return (
    <>
      <AppShell>
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/clients" element={<ClientsList />} />
          <Route path="/clients/new" element={<ClientCreate />} />
          <Route path="/clients/:id" element={<ClientEdit />} />
          <Route path="/scopes" element={<ScopesList />} />
          <Route path="/users" element={<UsersList />} />
          <Route path="/audit" element={<AuditList />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AppShell>
      <ToastViewport />
    </>
  );
}
