# Architecture Overview

## System diagram
```text
Browser (Web SPA)
  |  OIDC code flow + API calls
  v
AuthServer (OIDC authority, admin/governance APIs)
  |  access token validation/introspection contract
  v
Api (resource server + policy-map enforcement + internal token minting)
  |  short-lived internal token (+ optional mTLS)
  v
AppServer (internal app/content APIs)

Shared infra:
- PostgreSQL (AuthServer identity + OpenIddict + admin/governance state)
- Redis (events/cache/backplane paths)
```

## Service responsibilities
- **AuthServer**
  - OpenIddict authority endpoints (`/connect/*`, discovery, JWKS).
  - Interactive auth/account/self-service controllers.
  - Admin APIs for users/roles/permissions/clients/scopes/resources/governance.
  - Policy-map source endpoint used by Api refresh process.
- **Api**
  - Validates AuthServer access tokens.
  - Enforces required scope and policy-map-based permissions.
  - Mints internal tokens consumed by AppServer.
  - Hosts SignalR hub and Redis subscriber fanout.
  - Exposes `/health` and admin-protected `/ops/health`.
- **AppServer**
  - Internal `/app/*` routes with scope checks.
  - Accepts only Api-issued internal tokens.
  - Optional mTLS enforcement for inbound `/app` traffic.
  - Uses Hangfire + Redis-backed runtime integrations.
- **Web**
  - React/Vite SPA with OIDC client config (`response_type=code`).
  - User and admin routes, including admin surfaces tied to backend APIs.

## Shared libraries
- `Company.Auth.Contracts`: auth constants/claim names.
- `Company.Auth.Api`: authentication integration helpers for Api.
- `AstraId.Contracts`: event contracts/channels used by pub/sub paths.
- `AstraId.Logging`: logging/audit/redaction abstractions consumed by services.
