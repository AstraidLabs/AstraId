# AstraId

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)

AstraId is an open-source identity and authorization server built with ASP.NET Core Identity, OpenIddict, and a React-based web/admin UI.

## What is in this repository

- `src/AuthServer`: authorization server, ASP.NET Identity user management, `/auth/*` endpoints, `/connect/*` endpoints, admin APIs, and static hosting for `/admin` when built assets are present.
- `src/Api`: sample protected API with OpenIddict validation and permission-based authorization.
- `src/Web`: React + Vite UI for account flows and admin UI routes.
- `src/Company.Auth.Contracts`: shared auth constants/contracts.
- `src/Company.Auth.Api`: reusable API auth wiring for token validation and permission policies.

## ✨ Key features (code-backed)

### AuthServer
- ASP.NET Identity user store with confirmed account requirement and cookie session auth for first-party UI flows (`AstraId.Auth` cookie). (`src/AuthServer/Program.cs`)
- First-party auth endpoints under `/auth/*` for login, MFA challenge/setup, register, activation, forgot/reset password, logout, and session checks. (`src/AuthServer/Controllers/AuthController.cs`)
- Security/account APIs for profile, sessions, security events, privacy, and self-delete flows. (`src/AuthServer/Controllers/SelfServiceController.cs`, `src/AuthServer/Controllers/AccountPrivacyController.cs`, `src/AuthServer/Controllers/SelfDeleteController.cs`)

### OIDC / OAuth2
- OpenIddict endpoints: discovery, JWKS, authorize, token, userinfo, logout, revocation. (`src/AuthServer/Program.cs`)
- Server-level enabled flows: authorization code + refresh token. (`src/AuthServer/Program.cs`)
- Token endpoint handler contains additional handling for `client_credentials`, but this grant is not enabled in server configuration by default. (`src/AuthServer/Controllers/AuthorizationController.cs`, `src/AuthServer/Program.cs`)
- Scope/resource mapping via `AuthServerScopeRegistry` and claims enrichment with permission claims. (`src/AuthServer/Controllers/AuthorizationController.cs`, `src/AuthServer/Authorization/AuthServerScopeRegistry.cs`)

### Security governance
- Persisted signing key ring + JWKS materialization + hosted rotation worker and admin rotation/revoke endpoints. (`src/AuthServer/Services/SigningKeys`, `src/AuthServer/Controllers/Admin/AdminSigningKeysController.cs`, `src/AuthServer/Program.cs`)
- Token policy services and admin APIs for token/security policies. (`src/AuthServer/Services/Tokens`, `src/AuthServer/Controllers/Admin/AdminTokensController.cs`, `src/AuthServer/Controllers/Admin/AdminSecurityPoliciesController.cs`)
- Refresh token reuse detection/remediation and token incident logging APIs. (`src/AuthServer/Services/Tokens/RefreshTokenReuseDetectionService.cs`, `src/AuthServer/Services/Tokens/RefreshTokenReuseRemediationService.cs`, `src/AuthServer/Controllers/Admin/AdminTokenIncidentsController.cs`)
- Diagnostics and audit surfaces, including error log persistence/cleanup and admin diagnostics endpoints. (`src/AuthServer/Services/Diagnostics`, `src/AuthServer/Controllers/Admin/AdminDiagnosticsController.cs`, `src/AuthServer/Controllers/Admin/AdminAuditController.cs`)

### Admin UI / Admin APIs
- Admin API domain coverage includes clients, scopes, OIDC resources, API resources/endpoints, users, roles, permissions, signing keys, token policies, revocation, privacy/lifecycle policy, diagnostics, and email outbox. (`src/AuthServer/Controllers/Admin`)
- Admin SPA routes under `/admin/*` in the React app; AuthServer hosts `/admin` static files from `wwwroot/admin-ui` when available. (`src/Web/src/App.tsx`, `src/AuthServer/Program.cs`)

### API integration
- API validates tokens using OpenIddict validation (`AddCompanyAuth`) and enforces permission policies (`RequirePermission`). (`src/Company.Auth.Api/CompanyAuthExtensions.cs`, `src/Api/Program.cs`)
- API includes policy-map refresh integration with AuthServer and an endpoint-authorization middleware. (`src/Api/Program.cs`, `src/Api/Middleware/EndpointAuthorizationMiddleware.cs`, `src/Api/Services/PolicyMapRefreshService.cs`)

### Web UI scope
- Public/account routes (login/register/reset/activate/account/security/privacy) and admin routes are implemented in React Router. (`src/Web/src/App.tsx`)
- Web app uses `/auth/*` APIs and OIDC client settings for protocol integration. (`src/Web/src/api/authServer.ts`, `src/Web/src/auth/oidc.ts`)

## 🧭 Architecture overview

- Browser UI (`src/Web`) uses cookie-backed `/auth/*` APIs for first-party account flows.
- OAuth2/OIDC clients use `/connect/*` endpoints on AuthServer.
- Protected APIs (`src/Api`) validate access tokens against AuthServer issuer and enforce permission claims.
- Admin UI runs as React routes and consumes `/admin/api/*` endpoints.

Typical local dev ports from launch profiles:
- AuthServer: `https://localhost:7001`
- Api: `https://localhost:7002`
- Web: `http://localhost:5173`

## 🚀 Quickstart

### Prerequisites
- .NET SDK supporting `net10.0`
- Node.js + npm
- PostgreSQL (AuthServer EF Core store)

### Run all apps

Option A (script):
- Bash: `./scripts/dev.sh`
- PowerShell: `./scripts/dev.ps1`

Option B (manual):
1. `dotnet run --project src/AuthServer --launch-profile AuthServer`
2. `dotnet run --project src/Api --launch-profile Api`
3. `cd src/Web && npm install && npm run dev`

## ⚙️ Configuration (sections used by code)

### AuthServer (`src/AuthServer/appsettings*.json`)
- `ConnectionStrings:DefaultConnection`
- `AuthServer:Issuer`
- `AuthServer:UiMode`, `AuthServer:UiBaseUrl`
- `AuthServer:SigningKeys:*`
- `AuthServer:KeyRotationDefaults:*`
- `AuthServer:Tokens:*`
- `AuthServer:TokenPolicyDefaults:*`
- `AuthServer:GovernanceGuardrails:*`
- `AuthServer:Certificates:*` (encryption certificate loading)
- `AuthServer:DataProtection:*`
- `Email:*`
- `Diagnostics:*`
- `BootstrapAdmin:*`

### Api (`src/Api/appsettings*.json`)
- `Auth:Issuer`, `Auth:Audience`, `Auth:Scopes`
- `Api:AuthServer:*`
- `Services:AuthServer:*`, `Services:Cms:*`
- `Http:*`
- `Swagger:OAuthClientId`

> Do not commit real secrets in configuration values (DB passwords, SMTP passwords, API keys).

## 🔐 Security notes

- First-party web UX is cookie-based (`AstraId.Auth`), while API protection is token-based via OpenIddict validation.
- Production startup enforces HTTPS issuer for AuthServer.
- Email settings are required and validated at startup.
- Signing key rotation is implemented as a hosted service with persisted key metadata and admin management endpoints.
- Data protection key persistence can be configured to filesystem path and read-only mode.

## Duende vs AstraId (Truthful comparison)

- Feature: Authorization code flow — AstraId: Implemented — Notes: OpenIddict server enables authorization code and exposes `/connect/authorize` + `/connect/token`. — Evidence: `src/AuthServer/Program.cs`, `src/AuthServer/Controllers/AuthorizationController.cs`
- Feature: Refresh tokens — AstraId: Implemented — Notes: Refresh flow is enabled and guarded by token policy logic with absolute/sliding policy application. — Evidence: `src/AuthServer/Program.cs`, `src/AuthServer/Controllers/AuthorizationController.cs`, `src/AuthServer/Services/Tokens/TokenPolicyApplier.cs`
- Feature: Client credentials flow — AstraId: Partially — Notes: Token controller contains handling, but server configuration does not enable the grant by default. — Evidence: `src/AuthServer/Controllers/AuthorizationController.cs`, `src/AuthServer/Program.cs`
- Feature: Revocation endpoint — AstraId: Implemented — Notes: `/connect/revocation` is configured, with admin revocation APIs for user/client grant revocation. — Evidence: `src/AuthServer/Program.cs`, `src/AuthServer/Controllers/Admin/AdminRevocationController.cs`
- Feature: Introspection endpoint — AstraId: Missing — Notes: No introspection endpoint URI is configured; API sample uses JWT validation. — Evidence: `src/AuthServer/Program.cs`, `src/Company.Auth.Api/CompanyAuthExtensions.cs`
- Feature: Consent screen/persisted consent UX — AstraId: Missing — Notes: No consent controller/page or consent persistence flow appears in the web/auth endpoints. — Evidence: `src/AuthServer/Controllers/AuthorizationController.cs`, `src/Web/src/App.tsx`
- Feature: Logout propagation (front/back-channel SLO) — AstraId: Partially — Notes: End-session endpoint exists, but no explicit front-channel/back-channel logout propagation implementation is present. — Evidence: `src/AuthServer/Program.cs`, `src/AuthServer/Controllers/AuthorizationController.cs`
- Feature: Signing key management/rotation — AstraId: Implemented — Notes: Key ring, rotation worker, and admin rotate/revoke/jwks endpoints are present. — Evidence: `src/AuthServer/Services/SigningKeys`, `src/AuthServer/Controllers/Admin/AdminSigningKeysController.cs`, `src/AuthServer/Program.cs`
- Feature: Session/account security controls — AstraId: Implemented — Notes: Account session/security endpoints plus user lifecycle/inactivity/privacy governance APIs are included. — Evidence: `src/AuthServer/Controllers/SelfServiceController.cs`, `src/AuthServer/Controllers/Admin/AdminUserLifecycleController.cs`, `src/AuthServer/Controllers/Admin/AdminInactivityPolicyController.cs`, `src/AuthServer/Controllers/Admin/AdminPrivacyPolicyController.cs`

## Roadmap / gaps

See **Duende vs AstraId (Truthful comparison)** for parity-oriented gaps.

- Introspection endpoint is not implemented in the current server pipeline.
- Consent UX and persisted consent management are not implemented.
- Explicit front-channel/back-channel logout propagation is not implemented.
- External identity provider federation is not implemented in the current baseline.
- Distributed deployment hardening (shared cache/session strategy and operational playbooks) is not documented as implemented in this repo.

## License

No `LICENSE` file is currently present in this repository.
