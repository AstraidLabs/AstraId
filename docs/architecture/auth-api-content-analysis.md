# AuthServer <-> API <-> ContentServer analysis (Variant B)

## Current state discovered

Projects in solution:
- `src/AuthServer` (OpenIddict 7.2 auth server)
- `src/Api` (gateway/BFF-style API)
- `src/Company.Auth.Api` (shared auth validation wiring)
- `src/Company.Auth.Contracts` (shared constants)
- `src/ContentServer` (added in this change)

Authentication before this change:
- `AuthServer` issued JWT access tokens (access token encryption disabled, discovery/JWKS exposed).
- `Api` validated AuthServer tokens (JWT and optional introspection/hybrid support via `Company.Auth.Api`).
- No dedicated `ContentServer` boundary existed in the repo; API had only a CMS ping integration.

## AuthServer capability findings

- OpenIddict server is configured in `src/AuthServer/Program.cs` with issuer + discovery + JWKS + token/introspection/userinfo/logout/revocation endpoints.
- Access tokens are JWT (not opaque by default) because `DisableAccessTokenEncryption()` is enabled.
- Scopes/resources are seeded and now include `api`, `content.read`, and `content.write`.
- OpenIddict version remains `7.2.0` (unchanged).

## Is 3-tier architecture already supported?

Partially before this change (AuthServer + API existed), but **not fully**:
- API token validation existed.
- No strict ContentServer boundary with internal-token-only trust.
- No internal API-issued token flow from API to ContentServer.

## Gaps found

- Missing ContentServer component with isolated trust boundary.
- Missing internal short-lived token issuer in API.
- Missing enforcement that downstream accepts only API-issued internal tokens.
- Content scopes for read/write were not pre-seeded.

## Implemented changes

### 1) API internal token issuing + forwarding
- Added `InternalTokens` options in API config.
- Added internal JWT issuer service (`HS256`, short TTL constrained to 1-5 minutes).
- Added outbound handler that mints internal token per request and sets downstream bearer token.
- Added content authorization policies:
  - `content.read` for GET
  - `content.write` for POST
- Added API content forwarding endpoints:
  - `GET /content/items`
  - `POST /content/items`
- Added audit-safe forwarding logs (method/path only, no token value logging).

Changed files:
- `src/Api/Program.cs`
- `src/Api/Security/InternalTokenService.cs`
- `src/Api/Options/InternalTokenOptions.cs`
- `src/Api/Integrations/InternalTokenHandler.cs`
- `src/Api/Integrations/ContentServerClient.cs`
- `src/Api/appsettings.json`
- `src/Api/appsettings.Development.json`

### 2) ContentServer (new) validates INTERNAL tokens only
- Added new `src/ContentServer` ASP.NET Core API project.
- Configured JWT bearer validation to trust only internal token settings:
  - issuer = `InternalTokens:Issuer`
  - audience = `InternalTokens:Audience`
  - signature = `InternalTokens:SigningKey`
- Added explicit fail-closed rejection for AuthServer issuer (and any non-internal issuer).
- Added `ICurrentUser` abstraction for business-facing user context.
- Added endpoints:
  - `GET /health`
  - `GET /content/items` (`content.read`)
  - `POST /content/items` (`content.write`)

Changed files:
- `src/ContentServer/ContentServer.csproj`
- `src/ContentServer/Program.cs`
- `src/ContentServer/Security/InternalTokenOptions.cs`
- `src/ContentServer/Security/ScopeParser.cs`
- `src/ContentServer/Security/CurrentUser.cs`
- `src/ContentServer/appsettings.json`
- `src/ContentServer/appsettings.Development.json`
- `src/ContentServer/Properties/launchSettings.json`

### 3) AuthServer scopes/resources alignment
- Added `content.read` and `content.write` to allowed scopes and seeded clients/resources.

Changed files:
- `src/AuthServer/Authorization/AuthServerScopeRegistry.cs`
- `src/AuthServer/Seeding/AuthServerDefinitions.cs`

### 4) Solution and docs
- Added ContentServer project to solution.
- Added API README section for Variant B internal forwarding.
- Added root README section for local 3-tier wiring.

Changed files:
- `AstraId.sln`
- `src/Api/README.md`
- `README.md`

## Removed/forbidden coupling check

Search outcome:
- No ContentServer integration with AuthServer introspection/userinfo/JWKS.
- ContentServer trust is local internal token validation only.

## Smoke usage examples (placeholders only)

```bash
# Health
curl -k https://localhost:7002/health
curl -k https://localhost:7003/health

# API entrypoint with AuthServer access token (placeholder)
curl -k https://localhost:7002/content/items \
  -H "Authorization: Bearer <authserver_access_token>" \
  -H "X-Correlation-ID: <correlation-id>"

# Write through API (placeholder)
curl -k -X POST https://localhost:7002/content/items \
  -H "Authorization: Bearer <authserver_access_token>" \
  -H "Content-Type: application/json" \
  -d '{"title":"example"}'
```

## What is supported now (checklist)

- [x] API validates AuthServer end-user access tokens.
- [x] API enforces content read/write scope policies before downstream call.
- [x] API issues short-lived internal JWT (1-5 min) and forwards only that token.
- [x] ContentServer validates internal JWT issuer/audience/signature/lifetime.
- [x] ContentServer rejects AuthServer-issued tokens (fail-closed).
- [x] ContentServer business endpoints can use `ICurrentUser` abstraction.

## Missing / next steps

- Infrastructure key management/rotation for internal signing key (currently shared secret via config/env).
- Network policy hardening (ensure ContentServer is not publicly exposed, API is entrypoint).
- End-to-end integration tests once .NET SDK is available in CI/runtime.
- Optional migration to asymmetric internal signing + JWKS exposure from API for rotation at scale.
