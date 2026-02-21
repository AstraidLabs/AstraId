# AstraId

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![React](https://img.shields.io/badge/React-18-20232A?logo=react&logoColor=61DAFB)
![Vite](https://img.shields.io/badge/Vite-7-646CFF?logo=vite&logoColor=white)
![OpenID%20Connect](https://img.shields.io/badge/OpenID%20Connect-OAuth2%2FOIDC-2E77BC)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Database-4169E1?logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-Cache-DC382D?logo=redis&logoColor=white)

## 1) 🚀 Project Overview

AstraId is a multi-service identity and authorization solution built on ASP.NET Core + OpenIddict + React/Vite.

### Purpose, capabilities, and where to use AstraId

#### What this project is and its purpose

AstraId is a multi-service identity and access platform: it combines an OIDC/OAuth2 authority (AuthServer), a gateway/BFF-style API layer (Api), an internal application service (AppServer), and a React UI (Web). Its purpose is to centralize sign-in, authorization management, and secure identity propagation between the public edge and internal services (see *Components and responsibilities* below).

#### What it provides (practical capabilities)

- Provides an OIDC/OAuth2 authority including discovery, JWKS, and token/introspection/revocation endpoints (`/connect/*`).
- Covers identity flows for login, registration, and self-service account operations in AuthServer.
- Exposes admin APIs (`/admin/api/*`) for managing users, roles, permissions, clients, scopes/resources, and governance settings.
- Supports multiple access-token validation modes in Api (JWT, introspection, hybrid).
- Enforces scopes and a dynamic permission policy map fetched from AuthServer.
- Implements an internal token pattern: Api mints short-lived internal tokens for AppServer; AppServer validates API-issued internal tokens via Api JWKS (RS256/ES256, with optional temporary legacy HS256 fallback) and rejects AuthServer user tokens directly.
- Uses Redis across AuthServer/Api/AppServer runtime paths (cache/event-related scenarios per component).
- Uses Hangfire in AppServer for background jobs.
- Includes a realtime hub endpoint in Api (`/hubs/app`) for SignalR fan-out to clients.

#### Where to use it (typical scenarios)

- When you need SSO for multiple applications (SPA, internal tools, and other clients) behind a single authority.
- When you want centralized management of clients, scopes, roles, and permission models.
- In microservice architectures where internal services are not OIDC-aware: edge/API handles auth, internal calls use internal tokens.
- When adopting a BFF approach for SPA to enforce a stronger backend security boundary.
- When you need identity governance controls managed in one platform.
- When you need real-time propagation of updates/notifications through the API hub layer.

#### Short note on what it is not

- It is not a full CMS or business suite; AppServer is an internal application service focused on specific backend operations.
- It is not a hosted SaaS by default; it is self-hosted infrastructure you operate in your own environment.
- It is not a single-binary app; it is a composed multi-service architecture that is configured and run together.

### Components and responsibilities

- **AuthServer (`src/AuthServer`)**
  - Identity + login/registration/self-service account APIs.
  - OpenID Connect / OAuth2 authorization server (`/connect/*`, discovery, JWKS, token/introspection/revocation).
  - Admin APIs under `/admin/api/*` for users/roles/permissions/clients/scopes/resources/security/governance.
  - Bootstraps database schema and seed data at startup.
- **Api (`src/Api`)**
  - Public/protected API surface.
  - Validates AuthServer-issued access tokens (JWT / introspection / hybrid).
  - Enforces scopes + dynamic permission policy map fetched from AuthServer.
  - Proxies content operations to AppServer and mints short-lived internal tokens for AppServer.
- **AppServer (`src/AppServer`)**
  - Internal-only content/application service (`/app/*`).
  - Accepts **only** API-issued internal tokens (validated from Api JWKS, with optional temporary legacy HS256 fallback) and rejects AuthServer user tokens directly.
  - Uses Redis for cache/events and Hangfire for background jobs.
- **Web (`src/Web`)**
  - React + Vite UI with user and admin routes.
  - OIDC client integration (`authorization_code + PKCE`) against AuthServer.
  - Admin UI includes **Platform Health** (internal/admin only), which reads safe status data from Api `/ops/health`.

### High-level architecture (ASCII)

```text
+--------------------+            +-------------------+
|      Browser       |            |   Admin/User UI   |
|   (OIDC client)    |<---------->|   Web (Vite)      |
+---------+----------+            +---------+---------+
          |                                 |
          | OIDC authorize/token/userinfo   | HTTPS API calls
          v                                 v
+---------+---------------------------------+---------+
|                 AuthServer                          |
|  - Identity + OpenIddict + admin APIs              |
|  - Policy map source (/admin/apis/{api}/policy-map)|
+------------+---------------------------+------------+
             |                           |
             | bearer token validation   | policy-map + app calls
             v                           v
      +------+-------------------------------+
      |                 Api                  |
      |  - token validation                  |
      |  - /api/* and /hubs/app              |
      |  - mints internal token to AppServer |
      +----------------+----------------------+
                       |
                       | internal token (RS256/ES256 via JWKS)
                       v
               +-------+--------+
               |   AppServer    |
               |   /app/*       |
               +-------+--------+
                       |
          +------------+-------------+
          | PostgreSQL (AuthServer)  |
          | Redis (Auth/Api/App)     |
          +--------------------------+
```

### Default local URLs/ports

- AuthServer: `https://localhost:7001`.
- Api: `https://localhost:7002`.
- AppServer: `https://localhost:7003` and `http://localhost:5003`.
- Web (Vite): `http://localhost:5173`.



## Quick host-run scripts (no Docker)

- Dev (multi-terminal): `./scripts/astraid-dev.sh up` or `./scripts/astraid-dev.ps1 up`
- Prod-style host run + publish: `./scripts/astraid-prod.sh up` or `./scripts/astraid-prod.ps1 up`
- Stop launched services: `down` command on the matching script

See also: `docs/INSTALL-DEV.md`, `docs/INSTALL-PROD.md`, `docs/CONFIGURATION.md`, `docs/DATABASE.md`.
## 2) 🧰 Prerequisites

1. **.NET SDK 10.x** (all server projects target `net10.0`).
2. **Node.js + npm** for `src/Web` (repo does not pin exact Node version; use an active LTS range compatible with Vite 7, e.g. Node 20+).
3. **PostgreSQL** (AuthServer uses `Npgsql.EntityFrameworkCore.PostgreSQL`).
4. **Redis** (used across AuthServer/Api/AppServer runtime paths).
5. **Optional Docker** (for running Postgres/Redis locally).

### Local HTTPS certificate setup

```bash
dotnet dev-certs https --check
dotnet dev-certs https --trust
```

If trust fails on Linux, continue and trust in your distro/browser trust store manually.

## 3) 🛠️ Repository Setup

### 3.1 Clone / restore / build

```bash
git clone <REPO_URL>
cd AstraId
dotnet restore AstraId.sln
dotnet build AstraId.sln
```

### 3.2 Repository layout

- `src/AuthServer` – identity/OIDC/admin backend.
- `src/Api` – protected API/BFF-style gateway.
- `src/AppServer` – internal service reached via Api.
- `src/Web` – React frontend.
- `src/Company.Auth.Contracts` – shared auth claims/constants.
- `src/Company.Auth.Api` – reusable API authentication integration.
- `src/AstraId.Contracts` – shared event contracts.
- `scripts/dev.sh`, `scripts/dev.ps1` – convenience local startup scripts.

### Documentation

- [Development runbook](docs/runbook/development.md)
- [Production runbook](docs/runbook/production.md)
- [Security hardening](docs/security/hardening.md)
- [Architecture overview](docs/architecture/overview.md)
- [Architecture request flows](docs/architecture/request-flows.md)
- [IAM MVP scope](docs/iam/mvp.md)
- [IAM advanced OAuth features](docs/iam/advanced-oauth-features.md)
- [Platform checks](docs/ops/platform-checks.md)
- [Logging and audit](docs/ops/logging-and-audit.md)
- [Health and readiness](docs/ops/health-and-readiness.md)
- [Multi-tenant model](docs/tenancy/multi-tenant-model.md)

### 3.3 Admin UI build pipeline (AuthServer csproj target)

`src/AuthServer/AuthServer.csproj` contains target **`BuildAdminUi`** that:

1. Uses `../Web` as source.
2. Runs `npm ci` (if `node_modules` missing).
3. Runs `npm run build:admin`.
4. Copies `src/Web/dist-admin/**` into `src/AuthServer/wwwroot/admin-ui/**`.

Manual equivalent:

```bash
cd src/Web
npm ci
npm run build:admin
```

Then publish/build AuthServer so static admin assets are available under `/admin`.

## 4) 🔐 Configuration: How to Set It Up Safely

> **Security rule:** never commit real secrets to tracked `appsettings*.json`. Use placeholders in files and inject real values at runtime.

### Recommended secret sources

A) **`dotnet user-secrets`** (development).

B) **Environment variables** (development + production).

C) **External secret store** (production): e.g., platform-managed secret manager mounted as env vars or config provider.

---

### 4.1 AuthServer configuration reference

#### Section: `ConnectionStrings:DefaultConnection`
- **Purpose:** PostgreSQL connection for Identity/OpenIddict/auth data.
- **Required:** Yes.
- **Example:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=__REPLACE_ME__;Port=5432;Database=__REPLACE_ME__;Username=__REPLACE_ME__;Password=__REPLACE_ME__"
  }
}
```

- **Env var:** `ConnectionStrings__DefaultConnection`.

#### Section: `AuthServer:Issuer`, `UiMode`, `UiBaseUrl`, `HostedUiPath`
- **Purpose:** OIDC issuer + hosted/separate UI behavior.
- **Required:** `Issuer` required (must be absolute; HTTPS in production). Others optional depending on UI mode.
- **Example:**

```json
{
  "AuthServer": {
    "Issuer": "https://auth.example.com/",
    "UiMode": "Separate",
    "UiBaseUrl": "https://app.example.com",
    "HostedUiPath": "__REPLACE_ME_OPTIONAL__"
  }
}
```

- **Env vars:** `AuthServer__Issuer`, `AuthServer__UiMode`, `AuthServer__UiBaseUrl`, `AuthServer__HostedUiPath`.

#### Section: `AuthServer:Certificates` and `AuthServer:SigningKeys`
- **Purpose:** OIDC signing/encryption credentials and signing-key rotation ring settings.
- **Required:** signing capability required; certificate descriptors optional if using DB key ring mode.
- **Example:**

```json
{
  "AuthServer": {
    "Certificates": {
      "Signing": {
        "Path": "__REPLACE_ME__",
        "Password": "__REPLACE_ME__"
      },
      "Encryption": {
        "Path": "__REPLACE_ME__",
        "Password": "__REPLACE_ME__"
      }
    },
    "SigningKeys": {
      "Mode": "DbKeyRing",
      "Enabled": true,
      "RotationIntervalDays": 30,
      "PreviousKeyRetentionDays": 14,
      "CheckPeriodMinutes": 60,
      "Algorithm": "RS256",
      "KeySize": 2048
    }
  }
}
```

- **Env var examples:**
  - `AuthServer__Certificates__Signing__Path`
  - `AuthServer__Certificates__Signing__Password`
  - `AuthServer__SigningKeys__Mode`
  - `AuthServer__SigningKeys__RotationIntervalDays`

#### Section: `AuthServer:Tokens`, `AuthServer:TokenPolicyDefaults`
- **Purpose:** token lifetimes and refresh rotation/reuse detection policy.
- **Required:** Optional (defaults exist), but recommended explicit config in production.
- **Example:**

```json
{
  "AuthServer": {
    "Tokens": {
      "Public": {
        "AccessTokenMinutes": 30,
        "IdentityTokenMinutes": 15,
        "RefreshTokenAbsoluteDays": 30,
        "RefreshTokenSlidingDays": 7
      },
      "Confidential": {
        "AccessTokenMinutes": 60,
        "IdentityTokenMinutes": 30,
        "RefreshTokenAbsoluteDays": 60,
        "RefreshTokenSlidingDays": 14
      },
      "RefreshPolicy": {
        "RotationEnabled": true,
        "ReuseDetectionEnabled": true,
        "ReuseLeewaySeconds": 30
      }
    },
    "TokenPolicyDefaults": {
      "AccessTokenMinutes": 30,
      "IdentityTokenMinutes": 15,
      "AuthorizationCodeMinutes": 5,
      "RefreshTokenDays": 30,
      "RefreshRotationEnabled": true,
      "RefreshReuseDetectionEnabled": true,
      "RefreshReuseLeewaySeconds": 30,
      "ClockSkewSeconds": 60
    }
  }
}
```

#### Section: `AuthServer:GovernanceGuardrails`
- **Purpose:** min/max constraints for admin-editable governance values.
- **Required:** Optional but recommended.

#### Section: `Email` (SMTP)
- **Purpose:** outgoing email sender configuration.
- **Required:** Required for real email delivery; can be local SMTP sink in dev.
- **Example:**

```json
{
  "Email": {
    "Mode": "Smtp",
    "FromEmail": "no-reply@example.com",
    "FromName": "AstraId",
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "Username": "__REPLACE_ME__",
      "Password": "__REPLACE_ME__",
      "UseSsl": false,
      "UseStartTls": true,
      "TimeoutSeconds": 10
    }
  }
}
```

- **Env vars:** `Email__Smtp__Host`, `Email__Smtp__Username`, `Email__Smtp__Password`, etc.

### Email Sending (SMTP + Provider API)

AuthServer supports both SMTP and provider API-based email transports.

- Provider selection:
  - `Email:Provider = Smtp` (default behavior)
  - `Email:Provider = SendGrid` (modern HTTP API transport)
- Backward compatibility:
  - Existing `Email:Mode = Smtp` remains supported.

Configuration example (placeholders only):

```json
{
  "Email": {
    "Provider": "SendGrid",
    "FromEmail": "no-reply@example.com",
    "FromName": "AstraId",
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "Username": "__REPLACE_ME__",
      "Password": "__REPLACE_ME__",
      "UseSsl": false,
      "UseStartTls": true,
      "TimeoutSeconds": 10
    },
    "SendGrid": {
      "ApiKey": "__REPLACE_ME__"
    }
  }
}
```

`dotnet user-secrets` examples (keys only):

```bash
dotnet user-secrets set "Email:Provider" "SendGrid"
dotnet user-secrets set "Email:FromEmail" "__REPLACE_ME__"
dotnet user-secrets set "Email:SendGrid:ApiKey" "__REPLACE_ME__"
dotnet user-secrets set "Email:Smtp:Host" "__REPLACE_ME__"
dotnet user-secrets set "Email:Smtp:Username" "__REPLACE_ME__"
dotnet user-secrets set "Email:Smtp:Password" "__REPLACE_ME__"
```

Operational notes:
- SMTP remains fully supported and is still the default when provider is omitted.
- Provider APIs (e.g., SendGrid) are typically preferred for production deliverability/telemetry.
- Account for provider rate limits (`429`) and transient failures (`5xx`) with retries.
- Keep outbox/idempotency in place to avoid duplicate sends during retries and restarts.

#### Section: `Cors`
- **Purpose:** browser origin allow-list for credentialed requests.
- **Required:** yes for browser flows; wildcard not allowed by validation.
- **Example:**

```json
{
  "Cors": {
    "AllowedOrigins": ["https://app.example.com"],
    "AllowCredentials": true
  }
}
```

- **Env vars:** `Cors__AllowedOrigins__0`, `Cors__AllowCredentials`.

#### Section: `Redis`, `Hangfire`, `Diagnostics`, `SecurityHeaders`, `BootstrapAdmin`, `AuthServer:DataProtection`, `Notifications`
- **Purpose:** cache/event bus, jobs, diagnostics retention, headers/HSTS/CSP, optional bootstrap admin, DP key persistence, notification dispatcher behavior.
- **Required:**
  - `Redis:ConnectionString` required outside Development.
  - `BootstrapAdmin` optional (dev bootstrap convenience).
  - `AuthServer:DataProtection:KeyPath` strongly recommended for multi-instance production.
- **Example:**

```json
{
  "Redis": { "ConnectionString": "__REPLACE_ME__" },
  "Hangfire": { "StorageConnectionString": "__REPLACE_ME__" },
  "Diagnostics": {
    "ExposeToAdmins": true,
    "StoreErrorLogs": true,
    "MaxStoredDays": 14,
    "StoreDetailedExceptionDataInProduction": false
  },
  "SecurityHeaders": {
    "Enabled": true,
    "EnableHsts": true,
    "CspMode": "Enforce",
    "AllowedFrameAncestors": ["'none'"]
  },
  "BootstrapAdmin": {
    "Enabled": false,
    "OnlyInDevelopment": true,
    "RoleName": "Admin",
    "Email": "admin@example.com",
    "Password": "__REPLACE_ME__"
  },
  "AuthServer": {
    "DataProtection": {
      "KeyPath": "__REPLACE_ME__",
      "ReadOnly": false
    }
  },
  "Notifications": {
    "DispatcherBatchSize": 50,
    "DispatcherIntervalSeconds": 15,
    "MaxAttempts": 5,
    "BaseBackoffSeconds": 30,
    "MaxBackoffSeconds": 3600
  }
}
```

---

### 4.2 Api configuration reference

#### Section: `Auth`
- **Purpose:** token validation mode + issuer/audience/scope + introspection client credentials.
- **Required:** `InternalTokens:SigningKey` always required; outside Development issuer/audience/requiredScope validated as required.
- **Example:**

```json
{
  "Auth": {
    "ValidationMode": "Jwt",
    "Issuer": "https://auth.example.com/",
    "Audience": "api",
    "RequiredScope": "api",
    "Scopes": ["api", "content.read", "content.write"],
    "ClockSkewSeconds": 0,
    "Introspection": {
      "ClientId": "resource-api",
      "ClientSecret": "__REPLACE_ME__",
      "Scope": "api"
    }
  }
}
```

- **Env vars:** `Auth__ValidationMode`, `Auth__Issuer`, `Auth__Audience`, `Auth__Introspection__ClientSecret`, etc.

#### Section: `Api:AuthServer`
- **Purpose:** policy map refresh source (`admin/apis/{apiName}/policy-map`) + API key.
- **Required:** for policy map to refresh successfully, `BaseUrl`, `ApiName`, and `ApiKey` must be set.
- **Example:**

```json
{
  "Api": {
    "AuthServer": {
      "BaseUrl": "https://auth.example.com",
      "ApiName": "api",
      "ApiKey": "__REPLACE_ME__",
      "RefreshMinutes": 5
    }
  }
}
```

- **Env vars:** `Api__AuthServer__BaseUrl`, `Api__AuthServer__ApiName`, `Api__AuthServer__ApiKey`, `Api__AuthServer__RefreshMinutes`.

#### Section: `Services` (AuthServer/Cms/AppServer)
- **Purpose:** downstream base URLs + optional API keys + health check paths.
- **Required:** required for the corresponding integration/health check flows.

#### Section: `InternalTokens`
- **Purpose:** signs short-lived internal JWTs for AppServer and exposes internal JWKS for AppServer verification.
- **Required:** **Yes**, signing + JWKS settings are required for Api→AppServer token compatibility.
- **Example:**

```json
{
  "InternalTokens": {
    "Issuer": "astraid-api",
    "Audience": "astraid-app",
    "TokenTtlSeconds": 120,
    "AllowedServices": ["api"],
    "Signing": {
      "Algorithm": "RS256",
      "RotationEnabled": true,
      "RotationIntervalDays": 30,
      "PreviousKeyRetentionDays": 60,
      "KeySize": 2048
    },
    "Jwks": {
      "Enabled": true,
      "Path": "/internal/.well-known/jwks.json",
      "RequireInternalApiKey": true,
      "InternalApiKeyHeaderName": "X-Internal-Api-Key",
      "InternalApiKey": "__REPLACE_ME__"
    }
  }
}
```

- **Env vars:** `InternalTokens__Signing__Algorithm`, `InternalTokens__Jwks__InternalApiKey`, plus issuer/audience/TTL/rotation keys.

#### Section: `Redis`, `Cors`, `Swagger`, `SecurityHeaders`, `Http`
- **Purpose:** SignalR backplane, browser origins, OpenAPI, headers/HSTS, retry/timeout.
- **Required:** `Redis:ConnectionString` required by Api.

---

### 4.3 AppServer configuration reference

#### Section: `InternalTokens`
- **Purpose:** validates Api-issued internal tokens using Api JWKS.
- **Required:** **Yes** (`JwksUrl` and `JwksInternalApiKey` required; optional legacy HS256 fallback only for migration windows).

#### Section: `AuthServer:Issuer`
- **Purpose:** explicit guard to reject AuthServer user tokens when they appear as bearer tokens.
- **Required:** recommended/expected.

#### Section: `Redis`
- **Purpose:** distributed cache + event publisher.
- **Required:** yes.

#### Section: `Hangfire`
- **Purpose:** job storage connection string placeholder exists; current code uses in-memory Hangfire storage.
- **Required:** optional in current implementation, but production should use durable storage.

Example:

```json
{
  "InternalTokens": {
    "Issuer": "astraid-api",
    "Audience": "astraid-app",
    "AllowedServices": ["api"],
    "AllowedAlgorithms": ["RS256"],
    "JwksUrl": "https://localhost:7002/internal/.well-known/jwks.json",
    "JwksRefreshMinutes": 5,
    "JwksInternalApiKey": "__REPLACE_ME__",
    "AllowLegacyHs256": false,
    "LegacyHs256Secret": "__REPLACE_ME__"
  },
  "AuthServer": {
    "Issuer": "https://auth.example.com/"
  },
  "Redis": {
    "ConnectionString": "__REPLACE_ME__"
  }
}
```

---

### 4.4 Web/Vite environment variables

Create `src/Web/.env.local` (do not commit). Example:

```dotenv
VITE_API_BASE_URL=https://localhost:7002
VITE_AUTHSERVER_BASE_URL=https://localhost:7001
VITE_AUTH_AUTHORITY=https://localhost:7001
VITE_AUTH_CLIENT_ID=web-spa
VITE_AUTH_REDIRECT_URI=http://localhost:5173/auth/callback
VITE_AUTH_POST_LOGOUT_REDIRECT_URI=http://localhost:5173/
VITE_AUTH_SCOPE=openid profile email offline_access api content.read content.write
VITE_ADMIN_ENTRY_URL=http://localhost:5173/admin
# Optional override for admin API host:
# VITE_ADMIN_API_BASE_URL=https://localhost:7001
```

## 5) Development Setup (Step-by-step)

### Option A) No Docker (local Postgres + local Redis)

1. **Create Postgres DB + user** (adjust host/credentials):

```bash
psql -h localhost -U postgres -d postgres -c "CREATE USER astra_app WITH PASSWORD '__REPLACE_ME__';"
psql -h localhost -U postgres -d postgres -c "CREATE DATABASE astra OWNER astra_app;"
```

2. **Start Redis** via local service manager (systemctl/brew/services/etc).

3. **Set development secrets** (example using user-secrets):

```bash
# AuthServer
cd src/AuthServer
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=astra;Username=astra_app;Password=__REPLACE_ME__"
dotnet user-secrets set "BootstrapAdmin:Password" "__REPLACE_ME__"
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379,abortConnect=false"

# Api
cd ../Api
dotnet user-secrets init
dotnet user-secrets set "InternalTokens:Jwks:InternalApiKey" "__REPLACE_ME__"
dotnet user-secrets set "Api:AuthServer:ApiKey" "__REPLACE_ME__"
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379,abortConnect=false"

# AppServer
cd ../AppServer
dotnet user-secrets init
dotnet user-secrets set "InternalTokens:JwksInternalApiKey" "__REPLACE_ME__"
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379,abortConnect=false"
```

> Keep `InternalTokens:Jwks:InternalApiKey` (Api) and `InternalTokens:JwksInternalApiKey` (AppServer) aligned.

4. **Set Web env**:

```bash
cp src/Web/.env.example src/Web/.env.local
# then edit values if needed
```

5. **Run services** (separate terminals):

```bash
dotnet run --project src/AuthServer --launch-profile AuthServer
```

```bash
dotnet run --project src/Api --launch-profile Api
```

```bash
dotnet run --project src/AppServer --launch-profile AppServer
```

```bash
cd src/Web
npm install
npm run dev
```

### Option B) Docker for Postgres/Redis only

1. Start containers:

```bash
docker run -d --name astra-postgres \
  -e POSTGRES_PASSWORD=__REPLACE_ME__ \
  -e POSTGRES_DB=astra \
  -p 5432:5432 postgres:16

docker run -d --name astra-redis -p 6379:6379 redis:7
```

2. Use the same service startup and secrets steps from Option A.

### Option C) `docker-compose`

This repository does **not** contain a `docker-compose.yml`/`compose.yaml`. Use Option A or B.

### First-run behavior (important)

On AuthServer startup, hosted bootstrap service performs:

1. `Database.MigrateAsync()` (applies EF Core migrations).
2. Syncs permissions definitions.
3. Syncs API resources.
4. Syncs OpenIddict scopes.
5. Syncs OpenIddict client applications.
6. Syncs client state records.
7. Optionally creates/assigns bootstrap admin user/role based on `BootstrapAdmin` options.

Seeded defaults include:

- API resource: `api`.
- OIDC/public web client `web-spa` (redirect `http://localhost:5173/auth/callback`).
- Confidential client `resource-api` for machine-to-machine/introspection (development seed data; rotate/change for real deployments).

### Smoke checks

```bash
curl -k https://localhost:7001/.well-known/openid-configuration
```

```bash
curl -k -X POST https://localhost:7001/connect/token \
  -u <CLIENT_ID>:<CLIENT_SECRET> \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&scope=api"
```

```bash
curl -k https://localhost:7002/health
```

```bash
curl -k https://localhost:7003/health
```

```bash
curl -k -X POST "https://localhost:7002/hubs/app/negotiate?negotiateVersion=1" \
  -H "Authorization: Bearer <ACCESS_TOKEN>"
```

```bash
curl -k https://localhost:7002/api/admin/auth/contract \
  -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
```

Inspect `policyMapRefreshStatus` in the contract response (`ok` means refresh succeeded).

### Troubleshooting

- **HTTPS certificate trust errors**: rerun `dotnet dev-certs https --trust`; ensure browser trusts local cert.
- **CORS failures**: verify `Cors:AllowedOrigins` includes exact Web origin; avoid trailing slash mismatches.
- **DB migration/startup failure**: check `ConnectionStrings:DefaultConnection` and Postgres connectivity/credentials.
- **Redis connection errors**: verify `Redis:ConnectionString` in all services and Redis process availability.
- **Admin bundle missing** (`/admin` not loading in AuthServer): run `npm run build:admin` in `src/Web` and ensure files copied to `src/AuthServer/wwwroot/admin-ui`.
- **Policy map missing / 403 on `/api/*`**: Api middleware denies endpoints not present in policy map. Confirm `Api:AuthServer:ApiKey` matches AuthServer API resource key and `policyMapRefreshStatus` is `ok`.

## 6) Production Deployment (Step-by-step)

### Recommended topology

- **Public:** AuthServer, Api.
- **Internal-only (private network):** AppServer, Postgres, Redis.
- Do **not** expose AppServer directly to internet clients.

### TLS strategy

- Preferred: terminate TLS at reverse proxy/load balancer + forward HTTPS metadata headers to Kestrel.
- Alternative: direct Kestrel TLS with managed certificates.
- Ensure production issuer in AuthServer is HTTPS.

### Required production configuration baseline

1. Set explicit `Cors:AllowedOrigins` allow-list.
2. Keep secure cookie posture (`SecurePolicy=Always`, `HttpOnly`; already set in AuthServer cookie config).
3. Enable HSTS in non-development (`SecurityHeaders:EnableHsts=true`).
4. Keep rate limiting active (AuthServer global limiter includes sensitive auth/admin/token paths).
5. Replace in-memory Hangfire storage with durable backend for production resilience.
6. Configure production logging/observability; limit detailed exception data exposure.

### Key management & rotation operations

- AuthServer supports signing key ring and rotation settings (`AuthServer:SigningKeys`, `AuthServer:KeyRotationDefaults`).
- Operational guidance:
  1. Choose key mode (`Certificates` or `DbKeyRing`) explicitly.
  2. Back up persistent key material (DB records and/or cert private keys, plus data-protection keys path if used).
  3. Rotate keys on schedule (`RotationIntervalDays`) and keep old keys long enough for token/JWKS cache overlap (`PreviousKeyRetentionDays`, `JwksCacheMarginMinutes`).
  4. Verify JWKS publishes both active and rollover keys before retiring old keys.

### Email/SMTP in production

- Provide real SMTP host/credentials through env vars/secret store.
- Use STARTTLS/SSL per your mail provider.
- Keep SMTP passwords out of files and source control.

### Monitoring and operations

- Health endpoints:
  - AuthServer: `/health`
  - Api: `/health`
  - AppServer: `/health`
- Diagnostics/admin surfaces exist under `/admin/api/*` (admin authorization required).
- Backups:
  - PostgreSQL auth/config/audit data.
  - Signing keys + data-protection key material.
  - Any durable job storage used by Hangfire.

### Minimum Secure Production Checklist

- [ ] AuthServer issuer is HTTPS and externally correct.
- [ ] AppServer is private/internal only.
- [ ] All secrets sourced from environment/secret store (none in tracked files).
- [ ] `Cors:AllowedOrigins` is explicit and minimal.
- [ ] HSTS enabled for production.
- [ ] Rate limiting verified on sensitive endpoints.
- [ ] Durable Hangfire storage configured.
- [ ] Redis/Postgres restricted to internal network and authenticated.
- [ ] Signing key rotation policy configured and documented.
- [ ] Backup/restore runbook tested (DB + key material).

## 7) Admin Guide (for administrators)

### Accessing admin UI

- Hosted admin path in AuthServer: `https://<AUTH_HOST>/admin`.
- Admin API namespace: `https://<AUTH_HOST>/admin/api/*`.

### What admins can manage

- **Users/roles/permissions** via `/admin/api/users`, `/admin/api/roles`, `/admin/api/permissions`.
- **OIDC clients/scopes/resources** via `/admin/api/clients`, `/admin/api/oidc/scopes`, `/admin/api/oidc/resources`.
- **API policy map source data** via `/admin/api/api-resources` + endpoint synchronization (`/admin/api-sync/{apiName}/endpoints`).
- **Signing keys & rotation/security governance** via `/admin/api/signing-keys`, `/admin/api/security/*`.
- **Token governance / incidents / revocation** via `/admin/api/security/tokens`, `/admin/api/security/token-incidents`, `/admin/api/security/revoke/*`.
- **Diagnostics** via `/admin/api/diagnostics/errors` and email outbox endpoint.

### Example admin API requests (placeholders only)

```bash
curl -X GET "<BASE_URL>/admin/api/users" \
  -H "Authorization: Bearer <TOKEN>"
```

```bash
curl -X GET "<BASE_URL>/admin/api/clients" \
  -H "Authorization: Bearer <TOKEN>"
```

```bash
curl -X GET "<BASE_URL>/admin/apis/api/policy-map" \
  -H "X-Api-Key: <API_KEY>"
```

```bash
curl -X POST "<BASE_URL>/admin/api/security/revoke/client/<CLIENT_ID>" \
  -H "Authorization: Bearer <TOKEN>"
```

## 8) Developer Guide (for integrators)

### Third-party app integration model

1. Register client in AuthServer admin API/UI.
2. Configure redirect URIs and post-logout URIs exactly.
3. Use `authorization_code` with **PKCE** for browser/public clients.
4. Request only required scopes (e.g., `openid profile email api content.read`).
5. Call **Api** as the public integration target.
6. Do **not** call AppServer directly from third-party apps (AppServer expects internal token contract).

### Local development combinations

- **AuthServer + Web only:** useful for login/account/admin UI work.
- **AuthServer + Api:** useful for token validation, scope/policy map behavior.
- **Full stack (AuthServer + Api + AppServer + Web):** required for end-to-end `/app/items` flows.

### Debugging tips

- Check AuthServer discovery doc first.
- Use Api contract endpoint (`/api/admin/auth/contract`) to inspect issuer/audience/scheme/policy map refresh status.
- If `/api/*` returns 403 unexpectedly, verify both scope claims and policy map contents.
- If AppServer returns token errors, verify `InternalTokens` issuer/audience and JWKS/API-key alignment between Api and AppServer.

## 9) Appendix

### Ports table

| Component | Local URL(s) | Source |
|---|---|---|
| AuthServer | `https://localhost:7001` | `src/AuthServer/Properties/launchSettings.json` |
| Api | `https://localhost:7002` | `src/Api/Properties/launchSettings.json` |
| AppServer | `https://localhost:7003`, `http://localhost:5003` | `src/AppServer/Properties/launchSettings.json` |
| Web | `http://localhost:5173` | `src/Web/vite.config.ts` |

### URL endpoints table

| Service | Endpoints |
|---|---|
| AuthServer | `/.well-known/openid-configuration`, `/.well-known/jwks`, `/connect/authorize`, `/connect/token`, `/connect/introspect`, `/connect/userinfo`, `/connect/logout`, `/connect/revocation`, `/auth/*`, `/account/*`, `/admin/*`, `/health` |
| Api | `/api/public`, `/api/me`, `/api/admin/ping`, `/api/integrations/*`, `/api/admin/auth/contract`, `/hubs/app`, `/app/items`, `/internal/.well-known/jwks.json`, `/health` |
| AppServer | `/app/items`, `/app/items/{itemId}`, `/admin/hangfire`, `/health` |

### Environment variables quick reference

| Variable | Service |
|---|---|
| `ConnectionStrings__DefaultConnection` | AuthServer |
| `Redis__ConnectionString` | AuthServer / Api / AppServer |
| `AuthServer__Issuer` | AuthServer |
| `AuthServer__SigningKeys__Mode` | AuthServer |
| `AuthServer__SigningKeys__RotationIntervalDays` | AuthServer |
| `AuthServer__Tokens__Public__AccessTokenMinutes` | AuthServer |
| `Email__Smtp__Host` / `Email__Smtp__Username` / `Email__Smtp__Password` | AuthServer |
| `BootstrapAdmin__Email` / `BootstrapAdmin__Password` | AuthServer |
| `Auth__ValidationMode` / `Auth__Issuer` / `Auth__Audience` / `Auth__RequiredScope` | Api |
| `Auth__Introspection__ClientId` / `Auth__Introspection__ClientSecret` | Api |
| `Api__AuthServer__BaseUrl` / `Api__AuthServer__ApiName` / `Api__AuthServer__ApiKey` | Api |
| `InternalTokens__Jwks__InternalApiKey` | Api |
| `InternalTokens__JwksInternalApiKey` | AppServer |
| `Services__AppServer__BaseUrl` | Api |
| `VITE_API_BASE_URL` | Web |
| `VITE_AUTHSERVER_BASE_URL` / `VITE_AUTH_AUTHORITY` | Web |
| `VITE_AUTH_CLIENT_ID` / `VITE_AUTH_REDIRECT_URI` / `VITE_AUTH_SCOPE` | Web |

---

## Quick start command recap

```bash
# Terminal 1
dotnet run --project src/AuthServer --launch-profile AuthServer

# Terminal 2
dotnet run --project src/Api --launch-profile Api

# Terminal 3
dotnet run --project src/AppServer --launch-profile AppServer

# Terminal 4
cd src/Web
npm install
npm run dev
```

### Email Sending Providers (SMTP + APIs)

AuthServer now supports selecting the email transport with `Email:Provider`:

- `Smtp` (default when `Email:Provider` is missing)
- `SendGrid`
- `Mailgun`
- `Postmark`
- `Graph` (Microsoft 365 / Microsoft Graph `sendMail` with client credentials)

SMTP remains fully supported and is recommended for local development and fallback scenarios.
For production deliverability and provider-side analytics, API-based providers are generally preferred.

#### Provider selection

```json
{
  "Email": {
    "Provider": "Smtp"
  }
}
```

If `Email:Provider` is not set, AuthServer falls back to SMTP behavior.

#### SMTP configuration keys

```json
{
  "Email": {
    "Provider": "Smtp",
    "FromEmail": "<from-email>",
    "FromName": "<from-name>",
    "Smtp": {
      "Host": "<smtp-host>",
      "Port": 587,
      "Username": "<smtp-username>",
      "Password": "<smtp-password>",
      "UseSsl": false,
      "UseStartTls": true,
      "TimeoutSeconds": 10
    }
  }
}
```

#### SendGrid configuration keys

```json
{
  "Email": {
    "Provider": "SendGrid",
    "FromEmail": "<from-email>",
    "FromName": "<from-name>",
    "SendGrid": {
      "ApiKey": "<sendgrid-api-key>"
    }
  }
}
```

#### Mailgun configuration keys

```json
{
  "Email": {
    "Provider": "Mailgun",
    "FromEmail": "<from-email>",
    "FromName": "<from-name>",
    "Mailgun": {
      "ApiKey": "<mailgun-api-key>",
      "Domain": "<mailgun-domain>",
      "BaseUrl": "https://api.mailgun.net"
    }
  }
}
```

#### Postmark configuration keys

```json
{
  "Email": {
    "Provider": "Postmark",
    "FromEmail": "<from-email>",
    "FromName": "<from-name>",
    "Postmark": {
      "ServerToken": "<postmark-server-token>"
    }
  }
}
```

#### Graph configuration keys

```json
{
  "Email": {
    "Provider": "Graph",
    "FromEmail": "<from-email>",
    "FromName": "<from-name>",
    "Graph": {
      "TenantId": "<entra-tenant-id>",
      "ClientId": "<app-client-id>",
      "ClientSecret": "<app-client-secret>",
      "FromUser": "<mailbox-user-or-upn>"
    }
  }
}
```

#### `dotnet user-secrets` examples (keys only)

```bash
dotnet user-secrets set "Email:Provider" "<value>"
dotnet user-secrets set "Email:FromEmail" "<value>"
dotnet user-secrets set "Email:FromName" "<value>"

dotnet user-secrets set "Email:Smtp:Host" "<value>"
dotnet user-secrets set "Email:Smtp:Port" "<value>"
dotnet user-secrets set "Email:Smtp:Username" "<value>"
dotnet user-secrets set "Email:Smtp:Password" "<value>"

dotnet user-secrets set "Email:SendGrid:ApiKey" "<value>"

dotnet user-secrets set "Email:Mailgun:ApiKey" "<value>"
dotnet user-secrets set "Email:Mailgun:Domain" "<value>"
dotnet user-secrets set "Email:Mailgun:BaseUrl" "<value>"

dotnet user-secrets set "Email:Postmark:ServerToken" "<value>"

dotnet user-secrets set "Email:Graph:TenantId" "<value>"
dotnet user-secrets set "Email:Graph:ClientId" "<value>"
dotnet user-secrets set "Email:Graph:ClientSecret" "<value>"
dotnet user-secrets set "Email:Graph:FromUser" "<value>"
```

### Internal Tokens Migration: HS256 -> RS256/ES256 (Api -> AppServer)

Use a phased rollout for a controlled migration:
1. **Prepare AppServer for dual validation (non-prod first):**
   - Set `InternalTokens:AllowLegacyHs256=true` only while migrating.
   - Configure `InternalTokens:LegacyHs256Secret` via environment variables or user-secrets.
   - Configure JWKS settings (`InternalTokens:JwksUrl`, `InternalTokens:JwksInternalApiKey`).
2. **Enable Api asymmetric signing:**
   - Set `InternalTokens:Signing:Algorithm=RS256` (default) or `ES256`.
   - Keep short TTL with `InternalTokens:TokenTtlSeconds` (recommended 120; allowed 60-300).
   - Ensure Api JWKS is enabled at `InternalTokens:Jwks:Path` and protected by `InternalTokens:Jwks:InternalApiKey` header.
3. **Observe and verify:**
   - Confirm AppServer validates by `kid` from JWKS cache.
   - Verify `svc`/`azp` claim allowlist (`InternalTokens:AllowedServices`).
4. **Cutover:**
   - Set `InternalTokens:AllowLegacyHs256=false` everywhere.
   - In Production, keep `AllowLegacyHs256=false` as default.

Configuration keys (placeholders only):
- Api:
  - `InternalTokens:Issuer`, `InternalTokens:Audience`, `InternalTokens:TokenTtlSeconds`
  - `InternalTokens:Signing:Algorithm`, `RotationEnabled`, `RotationIntervalDays`, `PreviousKeyRetentionDays`, `KeySize`
  - `InternalTokens:Jwks:Enabled`, `Path`, `RequireInternalApiKey`, `InternalApiKeyHeaderName`, `InternalApiKey`
- AppServer:
  - `InternalTokens:Issuer`, `InternalTokens:Audience`, `AllowedServices`, `AllowedAlgorithms`
  - `InternalTokens:JwksUrl`, `JwksRefreshMinutes`, `JwksInternalApiKey`
  - `InternalTokens:AllowLegacyHs256`, `LegacyHs256Secret`

Operational guidance:
- Rotation runs with current+previous keys retained for overlap; AppServer refreshes JWKS periodically.
- Emergency rotation: rotate key immediately in Api and ensure AppServer JWKS refresh succeeds before disabling prior key retention.
- JWKS endpoint publishes **public keys only** and must remain internal-only at network level (private subnet, service mesh policy, firewall), in addition to header protection.
- Never commit secrets (`InternalApiKey`, `LegacyHs256Secret`) to source control.

### mTLS: Api ↔ AppServer

mTLS adds transport identity on top of JWT authorization:
- Api proves identity to AppServer with a client certificate.
- AppServer only accepts trusted Api certificates.

Enablement by environment:
- Api outbound (`AppServer:Mtls`):
  - `Enabled`
  - `ClientCertificate:Source|Path|Password|Thumbprint`
  - `ServerCertificate:ValidationMode` (`System` or `PinThumbprint`)
  - `ServerCertificate:PinnedThumbprints`
- AppServer inbound (`AppServer:Mtls`):
  - `Enabled`
  - `RequireClientCertificate`
  - `AllowedClientThumbprints`
  - `AllowedClientSubjectNames`

Certificate provisioning options:
- Development: short-lived self-signed certs generated locally, distributed out-of-band.
- Higher environments: internal CA-issued certs with rotation procedures and revocation management.

Troubleshooting:
- TLS handshake failures: verify client cert path/password, server trust chain, and hostname.
- 403 on `/app` endpoints: verify client cert allowlist thumbprint/subject settings.
- Pinning failures: verify configured pinned thumbprints exactly match server certificate.

Reverse proxy note:
- If TLS is terminated at ingress/proxy, enforce mTLS at that boundary.
- Do **not** trust forwarded client-certificate headers unless the trusted proxy boundary is strictly enforced.
- AppServer should be internal-only and not publicly exposed.

## Security Review & Production Hardening Notes

A code-based white-box penetration-style review was completed and documented in `docs/security/pen-test-code-review.md`.

Top risks identified (and addressed in this change set):
- Configuration drift risk for CORS/header/rate-limit controls across services.
- Uneven abuse-resistance defaults on sensitive endpoints (`/connect/*`, admin/integration APIs, hub traffic).
- Inconsistent response hardening headers/caching protections in downstream internal service responses.
- Partial sensitive-value masking patterns in exception diagnostics.

Key production controls now emphasized:
- `SecurityHardening:*` settings (enabled and strict by default in production) for CORS strict mode, security headers, and rate limiting.
- Tightened AuthServer partitioned rate limits for `/connect/token`, `/connect/introspect`, `/connect/revocation`, and auth flows.
- Added Api rate limiting partitions for admin/integration and hub paths.
- Added AppServer security header baseline and no-store caching on sensitive route families.
- Expanded diagnostics redaction patterns for additional token/API key name variants.

Operational notes:
- Keep real secrets in environment variables or user-secrets only (never in tracked JSON).
- Use explicit origin allowlists for any credentialed CORS configuration.
- Validate production configuration during deployment using startup option validation already present in services.

## Logging & Security Audit

- **Three streams / audiences**
  - **Ops/Admin runtime logs (Application stream):** normal operational runtime events for service health and behavior.
  - **Developer diagnostics (Dev stream):** verbose diagnostics intended for developers; enabled by default in Development and disabled by default in Production.
  - **Security audit stream:** structured minimal security events (`AstraId.Logging.SecurityAudit`) for auth decisions and security-relevant failures.

- **Development vs Production behavior**
  - `AstraLogging:Mode=Development` enables developer-friendly diagnostics (subject to per-stream flags).
  - `AstraLogging:Mode=Production` enforces safe defaults: redaction enabled, request-body logging disabled, query logging off by default, and dev stream disabled unless explicitly enabled.

- **Redaction and data handling guarantees**
  - Sensitive data is redacted before diagnostics persistence/logging, including sensitive headers (`Authorization`, `Cookie`, `Set-Cookie`, `X-Api-Key`, `X-Internal-Api-Key`) and sensitive query keys (`token`, `code`, `password`, `client_secret`, `refresh_token`, `access_token`, `id_token`, `assertion`).
  - Secrets/tokens/cookies/passwords/client secrets must never be logged in production logs.

- **Correlation and traceability**
  - `X-Correlation-ID` is enforced across services (generated when missing and returned in response headers).
  - Logs are enriched with correlation and trace context to support cross-service incident investigation.

## 13) 🔐 Additional IAM/OAuth feature flags

The following AuthServer capabilities are available behind configuration flags and are **disabled by default** unless explicitly enabled:

- **Device Code Flow**
  - Config: `AuthServer:DeviceFlow`
  - Keys: `Enabled`, `VerificationRateLimitPerMinute`, `PollingRateLimitPerMinute`

- **Token Exchange** (`urn:ietf:params:oauth:grant-type:token-exchange`)
  - Config: `AuthServer:TokenExchange`
  - Keys: `Enabled`, `AllowedClients`, `AllowedAudiences`, `ActorClaimType`

- **Refresh token reuse detection**
  - Config: `AuthServer:Tokens:RefreshPolicy` and `AuthServer:TokenPolicyDefaults`
  - Keys include: `RotationEnabled`, `ReuseDetectionEnabled`, `ReuseLeewaySeconds`, `RefreshReuseDetectionEnabled`, `RefreshReuseLeewaySeconds`

- **Session management (back-channel logout)**
  - Config: `AuthServer:SessionManagement`
  - Keys: `BackChannelEnabled`, `FrontChannelEnabled`, `BackChannelLogoutUrls`

## 🔐 OAuth Advanced Governance (Admin)

AstraId administrators can now configure advanced OAuth/IAM controls from the existing Admin UI at **Security → OAuth Advanced** and via the AuthServer admin API endpoint:

- `GET /admin/api/security/oauth-advanced-policy`
- `PUT /admin/api/security/oauth-advanced-policy`

This policy is database-backed, audited, and applied at runtime so changes take effect without restart in common paths. Safe defaults keep advanced features off until explicitly enabled. In production, enabling high-risk features (token exchange and front-channel logout) is guarded by break-glass confirmation.
