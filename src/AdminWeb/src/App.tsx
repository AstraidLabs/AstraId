import { Navigate, Route, Routes } from "react-router-dom";
import AppShell from "./components/AppShell";
import ToastViewport from "./components/ToastViewport";
import Dashboard from "./pages/Dashboard";
import ClientsList from "./pages/ClientsList";
import ClientCreate from "./pages/ClientCreate";
import ClientEdit from "./pages/ClientEdit";
import OidcScopesList from "./pages/OidcScopesList";
import OidcScopeCreate from "./pages/OidcScopeCreate";
import OidcScopeEdit from "./pages/OidcScopeEdit";
import OidcResourcesList from "./pages/OidcResourcesList";
import OidcResourceCreate from "./pages/OidcResourceCreate";
import OidcResourceEdit from "./pages/OidcResourceEdit";
import PermissionsList from "./pages/PermissionsList";
import PermissionCreate from "./pages/PermissionCreate";
import PermissionEdit from "./pages/PermissionEdit";
import RolesList from "./pages/RolesList";
import RoleEdit from "./pages/RoleEdit";
import ApiResourcesList from "./pages/ApiResourcesList";
import ApiResourceCreate from "./pages/ApiResourceCreate";
import ApiResourceEdit from "./pages/ApiResourceEdit";
import ApiResourceEndpoints from "./pages/ApiResourceEndpoints";
import UsersList from "./pages/UsersList";
import UserDetail from "./pages/UserDetail";
import AuditList from "./pages/AuditList";

export default function App() {
  return (
    <>
      <AppShell>
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/oidc/clients" element={<ClientsList />} />
          <Route path="/oidc/clients/new" element={<ClientCreate />} />
          <Route path="/oidc/clients/:id" element={<ClientEdit />} />
          <Route path="/oidc/scopes" element={<OidcScopesList />} />
          <Route path="/oidc/scopes/new" element={<OidcScopeCreate />} />
          <Route path="/oidc/scopes/:id" element={<OidcScopeEdit />} />
          <Route path="/oidc/resources" element={<OidcResourcesList />} />
          <Route path="/oidc/resources/new" element={<OidcResourceCreate />} />
          <Route path="/oidc/resources/:id" element={<OidcResourceEdit />} />
          <Route path="/config/permissions" element={<PermissionsList />} />
          <Route path="/config/permissions/new" element={<PermissionCreate />} />
          <Route path="/config/permissions/:id" element={<PermissionEdit />} />
          <Route path="/config/roles" element={<RolesList />} />
          <Route path="/config/roles/:id" element={<RoleEdit />} />
          <Route path="/config/api-resources" element={<ApiResourcesList />} />
          <Route path="/config/api-resources/new" element={<ApiResourceCreate />} />
          <Route path="/config/api-resources/:id" element={<ApiResourceEdit />} />
          <Route path="/config/api-resources/:id/endpoints" element={<ApiResourceEndpoints />} />
          <Route path="/users" element={<UsersList />} />
          <Route path="/users/:id" element={<UserDetail />} />
          <Route path="/audit" element={<AuditList />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AppShell>
      <ToastViewport />
    </>
  );
}
