import { Navigate, Outlet, Route, Routes } from "react-router-dom";
import TopNav from "./components/TopNav";
import Container from "./components/Container";
import Home from "./pages/Home";
import Login from "./pages/Login";
import Register from "./pages/Register";
import ForgotPassword from "./pages/ForgotPassword";
import ResetPassword from "./pages/ResetPassword";
import ActivateAccount from "./pages/ActivateAccount";
import AccountGuard from "./account/AccountGuard";
import AccountLayout from "./account/AccountLayout";
import OverviewPage from "./pages/account/OverviewPage";
import PasswordPage from "./pages/account/PasswordPage";
import EmailPage from "./pages/account/EmailPage";
import SessionsPage from "./pages/account/SessionsPage";
import MfaPage from "./pages/account/MfaPage";
import SecurityEventsPage from "./pages/account/SecurityEventsPage";
import SecurityPage from "./pages/account/SecurityPage";
import PrivacyPage from "./pages/account/PrivacyPage";
import ConfirmEmailChangePage from "./pages/account/ConfirmEmailChangePage";
import MfaChallenge from "./pages/MfaChallenge";
import NotFound from "./pages/NotFound";
import Error403 from "./pages/Error403";
import Error404 from "./pages/Error404";
import Error500 from "./pages/Error500";
import AdminLayout from "./admin/AdminLayout";
import AdminGuard from "./admin/AdminGuard";
import Dashboard from "./admin/pages/Dashboard";
import ClientsList from "./admin/pages/ClientsList";
import ClientCreate from "./admin/pages/ClientCreate";
import ClientEdit from "./admin/pages/ClientEdit";
import OidcScopesList from "./admin/pages/OidcScopesList";
import OidcScopeCreate from "./admin/pages/OidcScopeCreate";
import OidcScopeEdit from "./admin/pages/OidcScopeEdit";
import OidcResourcesList from "./admin/pages/OidcResourcesList";
import OidcResourceCreate from "./admin/pages/OidcResourceCreate";
import OidcResourceEdit from "./admin/pages/OidcResourceEdit";
import PermissionsList from "./admin/pages/PermissionsList";
import PermissionCreate from "./admin/pages/PermissionCreate";
import PermissionEdit from "./admin/pages/PermissionEdit";
import RolesList from "./admin/pages/RolesList";
import RoleEdit from "./admin/pages/RoleEdit";
import ApiResourcesList from "./admin/pages/ApiResourcesList";
import ApiResourceCreate from "./admin/pages/ApiResourceCreate";
import ApiResourceEdit from "./admin/pages/ApiResourceEdit";
import ApiResourceEndpoints from "./admin/pages/ApiResourceEndpoints";
import UsersList from "./admin/pages/UsersList";
import UserDetail from "./admin/pages/UserDetail";
import AuditList from "./admin/pages/AuditList";
import DiagnosticsErrorsList from "./admin/pages/DiagnosticsErrorsList";
import DiagnosticsErrorDetail from "./admin/pages/DiagnosticsErrorDetail";
import SigningKeys from "./admin/pages/SigningKeys";
import TokenPolicies from "./admin/pages/TokenPolicies";
import SecurityRotationPolicy from "./admin/pages/SecurityRotationPolicy";
import SecurityIncidents from "./admin/pages/SecurityIncidents";
import SecurityRevocation from "./admin/pages/SecurityRevocation";
import SecurityDataProtection from "./admin/pages/SecurityDataProtection";
import UserLifecyclePolicy from "./admin/pages/UserLifecyclePolicy";
import InactivityPolicy from "./admin/pages/InactivityPolicy";
import EmailOutbox from "./admin/pages/EmailOutbox";
import SecurityPrivacy from "./admin/pages/SecurityPrivacy";
import { adminRoutePattern, adminRoutePrefix } from "./routing";
import AnonymousOnlyRoute from "./auth/AnonymousOnlyRoute";

const PublicLayout = () => (
  <div className="min-h-screen bg-slate-950 text-slate-100">
    <TopNav />
    <Container>
      <main className="py-10">
        <Outlet />
      </main>
    </Container>
  </div>
);

const adminRootPath = adminRoutePrefix || "/";

const App = () => {
  return (
    <Routes>
      <Route element={<PublicLayout />}>
        <Route path="/" element={<Home />} />
        <Route path="/login" element={<AnonymousOnlyRoute><Login /></AnonymousOnlyRoute>} />
        <Route path="/mfa" element={<MfaChallenge />} />
        <Route path="/register" element={<AnonymousOnlyRoute><Register /></AnonymousOnlyRoute>} />
        <Route path="/forgot-password" element={<AnonymousOnlyRoute><ForgotPassword /></AnonymousOnlyRoute>} />
        <Route path="/reset-password" element={<ResetPassword />} />
        <Route path="/activate" element={<ActivateAccount />} />
        <Route
          path="/account"
          element={
            <AccountGuard>
              <AccountLayout />
            </AccountGuard>
          }
        >
          <Route index element={<OverviewPage />} />
          <Route path="profile" element={<Navigate to="/account" replace />} />
          <Route path="security" element={<SecurityPage />}>
            <Route path="mfa" element={<MfaPage />} />
            <Route path="email" element={<EmailPage />} />
            <Route path="password" element={<PasswordPage />} />
            <Route path="sessions" element={<SessionsPage />} />
            <Route path="activity" element={<SecurityEventsPage />} />
          </Route>
          <Route path="privacy" element={<PrivacyPage />} />
        </Route>
        <Route path="/account/email/confirm" element={<ConfirmEmailChangePage />} />
        <Route path="/error/403" element={<Error403 />} />
        <Route path="/error/404" element={<Error404 />} />
        <Route path="/error/500" element={<Error500 />} />
        <Route path="*" element={<NotFound />} />
      </Route>
      <Route
        path={adminRoutePattern}
        element={
          <AdminGuard>
            <AdminLayout />
          </AdminGuard>
        }
      >
        <Route index element={<Dashboard />} />
        <Route path="oidc/clients" element={<ClientsList />} />
        <Route path="oidc/clients/new" element={<ClientCreate />} />
        <Route path="oidc/clients/:id" element={<ClientEdit />} />
        <Route path="oidc/scopes" element={<OidcScopesList />} />
        <Route path="oidc/scopes/new" element={<OidcScopeCreate />} />
        <Route path="oidc/scopes/:id" element={<OidcScopeEdit />} />
        <Route path="oidc/resources" element={<OidcResourcesList />} />
        <Route path="oidc/resources/new" element={<OidcResourceCreate />} />
        <Route path="oidc/resources/:id" element={<OidcResourceEdit />} />
        <Route path="config/permissions" element={<PermissionsList />} />
        <Route path="config/permissions/new" element={<PermissionCreate />} />
        <Route path="config/permissions/:id" element={<PermissionEdit />} />
        <Route path="config/roles" element={<RolesList />} />
        <Route path="config/roles/:id" element={<RoleEdit />} />
        <Route path="config/api-resources" element={<ApiResourcesList />} />
        <Route path="config/api-resources/new" element={<ApiResourceCreate />} />
        <Route path="config/api-resources/:id" element={<ApiResourceEdit />} />
        <Route
          path="config/api-resources/:id/endpoints"
          element={<ApiResourceEndpoints />}
        />
        <Route path="users" element={<UsersList />} />
        <Route path="users/:id" element={<UserDetail />} />
        <Route path="audit" element={<AuditList />} />
        <Route path="diagnostics/errors" element={<DiagnosticsErrorsList />} />
        <Route path="diagnostics/errors/:id" element={<DiagnosticsErrorDetail />} />
        <Route path="diagnostics/email-outbox" element={<EmailOutbox />} />
        <Route path="security/keys" element={<SigningKeys />} />
        <Route path="security/rotation" element={<SecurityRotationPolicy />} />
        <Route path="security/tokens" element={<TokenPolicies />} />
        <Route path="security/incidents" element={<SecurityIncidents />} />
        <Route path="security/revocation" element={<SecurityRevocation />} />
        <Route path="security/dataprotection" element={<SecurityDataProtection />} />
        <Route path="security/user-lifecycle" element={<UserLifecyclePolicy />} />
        <Route path="security/inactivity" element={<InactivityPolicy />} />
        <Route path="security/privacy" element={<SecurityPrivacy />} />
        <Route path="security/signing-keys" element={<Navigate to="security/keys" replace />} />
        <Route path="security/token-policies" element={<Navigate to="security/tokens" replace />} />
        <Route
          path="*"
          element={<Navigate to={adminRootPath} replace />}
        />
      </Route>
    </Routes>
  );
};

export default App;
