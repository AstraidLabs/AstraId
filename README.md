# AstraId

Authorization and identity server solution built on ASP.NET Identity, OpenIddict, and a React admin/user UI.

## Introduction

AstraId is a repository containing:
- **AuthServer** (`src/AuthServer`): authentication, user lifecycle, OIDC server endpoints, and admin APIs.
- **Api** (`src/Api`): protected sample API using token validation and permission checks.
- **Web** (`src/Web`): React + Vite frontend for user and admin routes.
- **Company.Auth.Contracts** (`src/Company.Auth.Contracts`): shared auth constants and contracts.
- **Company.Auth.Api** (`src/Company.Auth.Api`): reusable API auth wiring for token validation/policies.

It is intended for developers building applications that need OIDC/OAuth2 authentication plus first-party account management and an admin surface.

## What it does (Description)

- Runs an OpenIddict server with discovery, JWKS, authorize, token, userinfo, end-session, and revocation endpoint URIs.
- Supports ASP.NET Identity-backed user authentication, registration, activation, password reset, and account session checks.
- Supports MFA flows (status, setup, confirm, recovery code regeneration, disable, MFA login challenge completion).
- Exposes self-service account APIs for password change, email change flow, sign out all sessions, security event listing, export, and deletion request/cancel.
- Provides admin APIs for OIDC clients/scopes/resources, API resources/endpoints, users, roles, permissions, diagnostics, audits, signing keys, token/security policies, revocation, lifecycle/privacy policies, and email outbox.
- Hosts admin static assets under `/admin` from `wwwroot/admin-ui` when built assets are present.
- Applies token policy settings (access/identity/refresh lifetimes and clock skew) through governance services and OpenIddict options configuration.
- Implements refresh-token reuse detection/remediation and incident logging for suspicious token events.
- Provides a protected API sample that validates bearer tokens with OpenIddict validation and applies permission checks, including a policy-map refresh from AuthServer.

## Key Features

### Authentication & user lifecycle
- `/auth` endpoints for login, register, activate, resend activation, forgot/reset password, logout, and session checks.
- MFA endpoints under `/auth/mfa/*` for setup and operation.
- Authenticated self-service endpoints for profile, password/email change, session revocation, security events, and privacy/deletion workflows.
- Cookie auth for first-party flows (`AstraId.Auth`) with `HttpOnly`, `SameSite=None`, and `SecurePolicy=Always`.

### OIDC/OAuth2
- OpenIddict endpoint URIs configured for:
  - `/.well-known/openid-configuration`
  - `/.well-known/jwks`
  - `/connect/authorize`
  - `/connect/token`
  - `/connect/userinfo`
  - `/connect/logout`
  - `/connect/revocation`
- Server flow enablement in OpenIddict configuration: **authorization_code** and **refresh_token**.
- `AuthorizationController` contains logic for `client_credentials`, but this grant is not enabled in the OpenIddict server registration in `Program.cs` (**Partial**).

### Admin & governance
- Admin APIs under `/admin/api/*` protected by `AdminOnly` policy.
- `AdminOnly` policy requires `Admin` role and enforces `system.admin` permission when permission claims are present.
- Signing key ring, rotation worker, and admin key management APIs exist (including JWKS rollover-oriented handling with active/previous keys).
- Token/security policy APIs exist with governance guardrails.
- Audit and diagnostics APIs exist; error logs can be stored and cleaned up by hosted services.

### API integration
- `Api` project validates tokens using `AddCompanyAuth` (OpenIddict validation, issuer + audience).
- Permission checks are enforced via policies and endpoint authorization middleware.
- Policy map is refreshed from AuthServer admin endpoint using API key auth.

## Prerequisites

- **.NET SDK** compatible with `net10.0` (AuthServer and Api target `net10.0`).
- **Node.js + npm** (for `src/Web` Vite frontend).
- **PostgreSQL** (AuthServer uses `Npgsql.EntityFrameworkCore.PostgreSQL`).

## Installation (local development)

1. Restore/build .NET projects:

```bash
dotnet restore
dotnet build
```

2. Run AuthServer (launch profile uses HTTPS 7001):

```bash
dotnet run --project src/AuthServer --launch-profile AuthServer
```

3. Run Api (launch profile uses HTTPS 7002):

```bash
dotnet run --project src/Api --launch-profile Api
```

4. Run Web (Vite dev server on port 5173):

```bash
cd src/Web
npm install
npm run dev
```

Default local dev URLs:
- AuthServer: `https://localhost:7001`
- Api: `https://localhost:7002`
- Web: `http://localhost:5173`

## Configuration

### Configuration overview

#### AuthServer (`src/AuthServer/appsettings*.json`)
- `ConnectionStrings:DefaultConnection`
- `AuthServer:Issuer`
- `AuthServer:UiMode`, `AuthServer:UiBaseUrl`, `AuthServer:HostedUiPath`
- `AuthServer:Certificates:*` (encryption/signing certificate descriptors)
- `AuthServer:SigningKeys:*`
- `AuthServer:KeyRotationDefaults:*`
- `AuthServer:Tokens:*`
- `AuthServer:TokenPolicyDefaults:*`
- `AuthServer:GovernanceGuardrails:*`
- `AuthServer:DataProtection:*`
- `Cors:AllowedOrigins`
- `Email:*`
- `Diagnostics:*`
- `BootstrapAdmin:*`

Example (placeholders only):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=astra;Username=<user>;Password=<password>"
  },
  "AuthServer": {
    "Issuer": "https://localhost:7001/",
    "UiMode": "Separate",
    "UiBaseUrl": "http://localhost:5173",
    "SigningKeys": {
      "Mode": "Auto",
      "RotationIntervalDays": 30,
      "PreviousKeyRetentionDays": 14
    },
    "TokenPolicyDefaults": {
      "AccessTokenMinutes": 30,
      "IdentityTokenMinutes": 15,
      "RefreshTokenDays": 30,
      "ClockSkewSeconds": 60
    }
  },
  "Email": {
    "FromEmail": "no-reply@example.com",
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "Username": "<smtp-user>",
      "Password": "<smtp-password>"
    }
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

#### Api (`src/Api/appsettings*.json`)
- `Auth:Issuer`, `Auth:Audience`, `Auth:Scopes`
- `Api:AuthServer:*` (policy-map source)
- `Services:AuthServer:*`, `Services:Cms:*`
- `Http:*`
- `Swagger:OAuthClientId`

Example:

```json
{
  "Auth": {
    "Issuer": "https://localhost:7001/",
    "Audience": "api",
    "Scopes": ["api"]
  },
  "Api": {
    "AuthServer": {
      "BaseUrl": "https://localhost:7001",
      "ApiName": "api",
      "ApiKey": "<api-key>",
      "RefreshMinutes": 5
    }
  }
}
```

#### Web (`src/Web/.env.example`)
- `VITE_API_BASE_URL`
- `VITE_AUTHSERVER_BASE_URL`
- `VITE_AUTH_AUTHORITY`
- `VITE_AUTH_CLIENT_ID`
- `VITE_AUTH_REDIRECT_URI`
- `VITE_AUTH_POST_LOGOUT_REDIRECT_URI`
- `VITE_AUTH_SCOPE`
- `VITE_ADMIN_ENTRY_URL`

## AstraId vs Duende (Truthful comparison)

- AstraId implements OIDC discovery/JWKS/authorize/token/userinfo/logout/revocation endpoint wiring in code. Duende typically provides OIDC/OAuth endpoint infrastructure as part of a commercial identity server offering.
- AstraId implements authorization code + refresh flows in OpenIddict server setup. Duende typically supports core OAuth2/OIDC flows with configurable clients/scopes.
- AstraId has refresh-token reuse detection and remediation logic (including token/authorization revocation and incident logging). Duende enterprise offerings typically include advanced token/security controls.
- AstraId has an admin API/UI surface for clients, scopes, resources, users, roles, and permissions. Duende typically provides admin/operational tooling via ecosystem and commercial components.
- AstraId includes signing key ring and rotation workers with active/previous key handling for JWKS rollover. Duende typically includes key management options in enterprise deployments.
- AstraId includes diagnostics and audit APIs backed by persisted logs. Duende deployments typically include operational observability options via platform integrations.
- AstraId includes first-party account lifecycle APIs (registration, activation, password reset, MFA, sessions). Duende is generally protocol-focused and commonly paired with custom account UX.
- AstraId includes a sample protected API that validates tokens and enforces permission mapping. Duende commonly integrates with APIs via standards-based token validation.

## What AstraId is NOT

- Not a CMS platform.
- Not a hosted multi-tenant identity SaaS product in this repository.
- Not a full federation hub for external identity providers (no external IdP federation implementation found).
- Not a full replacement for all Duende enterprise capabilities out of the box.
- **Not implemented:** introspection endpoint configuration.
- **Not implemented:** explicit consent UI/persisted consent flow.
- **Partial:** `client_credentials` token handling logic exists in controller, but grant is not enabled in OpenIddict server options.

## Security notes

- In production, AuthServer startup enforces HTTPS for `AuthServer:Issuer`.
- Auth cookie settings are explicitly hardened (`HttpOnly`, `SecurePolicy=Always`, `SameSite=None`).
- Token policy defaults include access/identity/refresh lifetimes and clock skew configuration.
- Refresh token rotation/reuse detection settings are configurable and checked during refresh exchanges.
- Signing keys can be certificate-based or DB key-ring based (mode-resolved), with hosted rotation checks and key retention/grace handling.
- Data Protection keys can be persisted to filesystem and optionally set read-only.

## License

No `LICENSE` file is currently present in this repository.
