# AuthServer OIDC/OAuth2 Production Readiness Report

## Repository Mapping (Architecture & Where to Change What)

### Projects/Layers
- **AuthServer** (`src/AuthServer`): OpenIddict-based authorization server, identity UI, admin APIs, EF Core context/migrations.
- **Company.Auth.Contracts** (`src/Company.Auth.Contracts`): Shared auth constants/contract DTOs.
- **Api/Web/Company.Auth.Api**: Downstream consumers (not modified).

### Core Auth/OIDC Configuration
- **OpenIddict server configuration**: `src/AuthServer/Program.cs`
- **Authorization/token endpoints**: `src/AuthServer/Controllers/AuthorizationController.cs`
- **OpenIddict seeding (scopes/clients)**: `src/AuthServer/Seeding/AuthBootstrapHostedService.cs` and `AuthServerDefinitions.cs`
- **Admin OIDC client CRUD**: `src/AuthServer/Controllers/Admin/AdminClientsController.cs`, `src/AuthServer/Services/Admin/AdminClientService.cs`

### User Management Flows
- **Identity + login/registration**: `src/AuthServer/Program.cs` (Identity setup) + Razor Pages under `src/AuthServer/Pages`
- **Email confirmation/reset**: `src/AuthServer/Pages` + `IEmailSender` via `SmtpEmailSender`
- **Admin users/roles**: `src/AuthServer/Services/Admin` and controllers under `src/AuthServer/Controllers/Admin`

### Where to Change What (Quick Map)
- **Endpoints / flow behavior**: `AuthorizationController.cs`
- **OpenIddict server options (issuer, endpoints, flows, certificates)**: `Program.cs`
- **Client registration validation/permissions**: `AdminClientService.cs`
- **Scope definitions / claims mapping**: `Authorization/AuthServerScopeRegistry.cs`
- **CORS policy per client**: `Services/Cors/ClientCorsPolicyProvider.cs`

---

## OIDC/OAuth2 Readiness Audit (Third-Party Clients)

| Endpoint | Status | Notes |
|---|---|---|
| Discovery (`/.well-known/openid-configuration`) | ✅ | Explicitly configured in `Program.cs`. |
| JWKS (`/.well-known/jwks`) | ✅ | Explicitly configured in `Program.cs`. |
| Authorization (`/connect/authorize`) | ✅ | Implemented via `AuthorizationController`. |
| Token (`/connect/token`) | ✅ | Implemented via `AuthorizationController`. |
| End-session (`/connect/logout`) | ✅ | Implemented via `AuthorizationController`. |
| Revocation (`/connect/revocation`) | ✅ | Enabled and allowed for token-based clients. |
| Userinfo (`/connect/userinfo`) | ✅ | Implemented in `AuthorizationController`. |
| Introspection (`/connect/introspect`) | ⚠️ | Not enabled (optional). If resource servers require it, add endpoint + permissions. |

---

## Findings & Risks (Priority)

### Critical
- **Persistent signing/encryption keys were dev-only**: dev certificates were always used, which is unsafe for production. Fixed by loading certificates from configuration/environment with a strict production requirement.  

### High
- **Per-client CORS allowlist missing**: CORS relied on a static origin list only. Now dynamically allowlisted based on registered client redirect/post-logout URIs (plus configured origins), with caching.
- **PKCE requirement not enforced per public client**: Now enforced server-side for public clients (authorization code flow).
- **Revocation endpoint not enabled**: Now configured and permissioned for relevant clients.

### Medium
- **Scope/claims mapping scattered**: Centralized scope registry and claim destination mapping introduced.
- **Weak redirect URI validation**: Now enforces HTTPS (except loopback HTTP) and forbids URI fragments.

### Low
- **Diagnostics**: Added correlation IDs and structured, safe logging for auth failures.

---

## Security Requirements Checklist (Maintainer Use)

**Flows**
- [x] Authorization Code Flow enabled.
- [x] Implicit Flow disabled.
- [x] Resource Owner Password Credentials disabled.
- [x] PKCE required for public clients (server-side enforced).

**Redirects & Tokens**
- [x] Strict redirect URI matching (absolute, no fragments, HTTPS unless loopback).
- [x] Token issuer is HTTPS in production.
- [x] Revocation endpoint enabled.
- [x] Claims mapped by scope with explicit destinations.

**Operational**
- [x] Persistent signing & encryption certificates required in production.
- [x] Per-client CORS allowlist derived from registered URIs.
- [x] Correlation ID added to logs/response.

**Optional / Future**
- [ ] Introspection endpoint enabled if resource servers need it.
- [ ] Refresh token reuse detection/rotation verification (OpenIddict default rolling tokens are recommended—confirm in production).

---

## Configuration Notes (Environment Variables / Safe Placeholders)

> **Do not store secrets in appsettings.** Use environment variables or secret stores.

### Issuer
- `AuthServer__Issuer=https://auth.example.com/`

### Signing/Encryption Certificates
You can use **one** of the following per certificate:
- **PFX File**
  - `AuthServer__Certificates__Signing__Path=/secrets/signing.pfx`
  - `AuthServer__Certificates__Signing__Password=***`
  - `AuthServer__Certificates__Encryption__Path=/secrets/encryption.pfx`
  - `AuthServer__Certificates__Encryption__Password=***`
- **Base64 PFX**
  - `AuthServer__Certificates__Signing__Base64=***`
  - `AuthServer__Certificates__Signing__Password=***`
- **Windows Store**
  - `AuthServer__Certificates__Signing__Thumbprint=ABCDEF...`
  - `AuthServer__Certificates__Signing__StoreName=My`
  - `AuthServer__Certificates__Signing__StoreLocation=CurrentUser`

> If `Encryption` is not specified, the signing certificate is reused for encryption. In production, at least a signing cert is required.

### CORS (Optional Static Allowlist)
- `Cors__AllowedOrigins__0=https://spa.example.com`
- `Cors__AllowedOrigins__1=https://admin.example.com`

---

## Summary of Code Changes Implemented
- **Centralized scope registry and claim destination mapping.**
- **Per-client CORS allowlist provider with cache.**
- **Strict redirect URI validation + PKCE enforcement for public clients.**
- **Revocation endpoint enabled and permissioned.**
- **Production certificate loading via environment configuration.**
- **Correlation ID middleware and safe structured logging for auth failures.**

