# AstraId

AstraId is an open-source Identity & Authorization platform built on **ASP.NET Core**, **ASP.NET Identity**, and **OpenIddict**.  
It provides an **OIDC/OAuth2 authorization server**, a **first-party auth API for UI flows**, an **admin UI/API**, and a sample/resource **API project** that validates tokens and enforces permissions.

> This project is **not Duende IdentityServer**. It is an OpenIddict-based implementation with its own admin & governance layer.

---

## What AstraId is

- **Authorization Server** (OIDC/OAuth2) using OpenIddict  
- **User identity** using ASP.NET Identity (users, roles, claims, lockout, tokens)
- **First-party auth endpoints** for Web UI flows (`/auth/*`) using cookie session
- **Admin area** (SPA served under `/admin`) + Admin APIs (`/admin/api/*`)
- **Governance features** for token policies, security events, and key rotation (project-specific)
- **Resource API** sample project validating access tokens and enforcing endpoint permissions

## What AstraId is not

- Not a Duende IdentityServer fork
- Not an external IdP aggregator (no external providers unless you add them)
- Not a “generic UI theme pack” — the focus is security, governance, and repeatable auth features

---

## Solution layout

- `src/AuthServer`  
  OpenIddict server + ASP.NET Identity + admin APIs + UI hosting (public + admin)
- `src/Api`  
  Sample/resource API using OpenIddict validation + permission enforcement
- `src/Web`  
  React (Vite) UI: login/register/MFA/account + admin SPA routes
- `Company.Auth.Contracts`, `Company.Auth.Api`  
  Shared contracts + API auth/authorization helpers

---

## Local development (high level)

### Prerequisites
- .NET SDK (matching the solution)
- Node.js + npm (for `src/Web`)
- PostgreSQL (AuthServer DB)
- SMTP dev server (optional but recommended for local email flows)

### Run
1. Start **AuthServer** (default dev: `https://localhost:7001`)
2. Start **Api** (default dev: `https://localhost:7002`)
3. Start **Web** (Vite dev server, default: `http://localhost:5173`)

> The Web app talks to AuthServer via `/auth/*` (cookie session) and uses OIDC endpoints under `/connect/*` for OAuth2/OIDC flows.

---

## Core endpoints

### First-party auth flows (`/auth/*`)
- `POST /auth/login`
- `POST /auth/login/mfa`
- `POST /auth/register`
- `POST /auth/activate`
- `POST /auth/resend-activation`
- `POST /auth/forgot-password`
- `POST /auth/reset-password`
- `POST /auth/logout`
- `GET  /auth/session`
- MFA management:
  - `GET  /auth/mfa/status`
  - `POST /auth/mfa/setup/start`
  - `POST /auth/mfa/setup/confirm`
  - `POST /auth/mfa/recovery-codes/regenerate`
  - `POST /auth/mfa/disable`

### OIDC/OpenIddict protocol surface (`/connect/*`)
- `GET  /connect/authorize`
- `POST /connect/token`
- `GET  /connect/userinfo`
- `GET  /connect/logout`
- `POST /connect/revocation` (configured)

Discovery & JWKS:
- `/.well-known/openid-configuration`
- `/.well-known/jwks`

---

## Authentication model (important)

AstraId currently supports two complementary layers:

1) **Cookie session** for the first-party UI (`/auth/*`)  
   Used by the Web UI (credentials include), useful for account screens, admin area, and interactive flows.

2) **OIDC/OAuth2 tokens** for APIs (`/connect/*`)  
   Used by the resource API (`src/Api`) via OpenIddict validation.

This is a valid architecture for an “all-in-one” platform: browser UX on cookies + API authorization on tokens.

---

## Security & governance features (implemented)

- Email confirmation + password reset via SMTP templates
- MFA (TOTP) + recovery codes + remember device
- Lockout on failed sign-in (Identity)
- Rate limiting for sensitive endpoints (project-level implementation)
- Token policy settings (lifetimes, clock skew, refresh behavior)
- Refresh token security controls (rotation/reuse detection) when enabled by policy
- Signing keys stored/managed with persistence (project key ring tables) + rotation policy metadata
- Diagnostics & error log persistence + cleanup job
- Audit logging for admin operations

> Exact feature behavior is governed by configuration and policy rows stored in the database (admin surface can manage these).

---

## Admin area

- Admin UI is served under `/admin` (SPA + fallback routing when built assets exist)
- Admin APIs live under `/admin/api/*`
- Typical admin domains:
  - Users, roles, permissions
  - OIDC clients/scopes/resources
  - API resources + endpoint permission maps
  - Audit logs + diagnostics
  - Security governance (token policy, key rotation policy, incidents)

---

## Known limitations / not yet implemented

These are intentionally called out to avoid misleading claims:

- `client_credentials` flow is not enabled by default (add if you need service-to-service tokens)
- Consent UI / persisted consent is not part of the baseline UX
- Front-channel / back-channel logout propagation is not fully implemented as a Duende-like SSO feature
- Introspection endpoint is not enabled by default (JWT validation is used in the API sample)

---

## Contributing

PRs are welcome. If you plan a bigger change (new OIDC flow, multi-tenant, consent, etc.), please open an issue/discussion first so the design stays consistent.

---

## License

TBD (choose and add a LICENSE file if you want public reuse clarity).
