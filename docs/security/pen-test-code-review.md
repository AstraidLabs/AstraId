# AstraId White-Box Penetration-Style Security Code Review

## Executive summary
This review assessed the `AuthServer`, `Api`, `AppServer`, and `Web` projects from source/config only (no active scanning). The codebase already contains strong baseline controls (strict cookie flags, issuer/audience checks, internal token service claims, admin authorization policies, and diagnostics governance), but there were still hardening opportunities around configurable security enforcement boundaries and abuse controls.

Primary risk themes:
1. **Operational drift risk** (security controls present but not centrally switchable by environment policy).
2. **Abuse resistance gaps** (AuthServer and Api limits could be tuned/partitioned more explicitly for sensitive paths).
3. **Header and cache policy consistency** (AppServer lacked a baseline security header middleware equivalent to AuthServer/Api).
4. **Diagnostic redaction completeness** (sensitive-key matching was good, but not covering all token/API key key names).

All implemented fixes are low-risk guardrails and defaults; they do not change business/auth flows.

## Threat model (high-level)

### AuthServer
- **External surface:** `/connect/*`, `/auth/*`, `/account/*`, `/admin/*`, `/health`, static admin UI.
- **Trust boundary:** browser/user-agent to identity core; OpenIddict endpoints for OAuth/OIDC issuance and management.
- **Sensitive assets:** user credentials, refresh tokens, signing keys, policy configuration, admin endpoints.
- **Primary threats:** credential stuffing, token endpoint abuse, misconfigured credentialed CORS, diagnostic data leakage.

### Api
- **External surface:** `/api/*`, `/app/*` proxy operations, `/hubs/app`, `/internal/.well-known/jwks.json`, `/health`.
- **Trust boundary:** browser/API consumers to protected resource server; downstream trust to AppServer.
- **Sensitive assets:** bearer tokens, policy map, internal token keys/JWKS, service API keys.
- **Primary threats:** authorization bypass via policy drift, downstream over-privilege, CORS misconfiguration, hub flood abuse.

### AppServer
- **External surface:** `/app/*`, `/admin/hangfire`, `/health`.
- **Trust boundary:** Api-to-AppServer internal JWT boundary (+ optional mTLS boundary).
- **Sensitive assets:** internal signing verification trust, Redis event/cache data, Hangfire jobs.
- **Primary threats:** internal token forgery/algorithm abuse, service identity spoofing, admin dashboard exposure.

### Web
- **External surface:** SPA routes and admin routes with OIDC login integration.
- **Trust boundary:** browser runtime + token/cookie handling in frontend.
- **Sensitive assets:** OIDC session state, user profile/admin UI access context.
- **Primary threats:** token leakage in browser context, misrouted admin navigation, cross-origin interaction errors.

## Recon inventory summary
- **Auth modes:** cookie auth (AuthServer UI/account), bearer token auth (Api/AppServer), introspection mode support in Api, internal Api→AppServer JWT.
- **Datastores:** Postgres via EF Core (AuthServer), Redis cache/pubsub (all service roles), Hangfire in-memory storage in AuthServer/AppServer.
- **Privileged entry points:** `/admin/api/*`, `/admin/hangfire`, `/connect/token`, `/connect/introspect`, `/connect/revocation`, key/JWKS endpoints.
- **Trust boundaries:** browser↔AuthServer, browser↔Api, Api↔AuthServer policy/introspection, Api↔AppServer internal JWT (+ optional mTLS), Redis pub/sub.

## Findings table

| ID | Severity | Evidence | Risk / exploitability | Remediation |
|---|---|---|---|---|
| F-01 | Medium | `src/AuthServer/Program.cs` (CORS policy + credentials), `CorsOptions` validation | Credentialed CORS safety depended on per-section config with no explicit hardening mode switch; production drift could re-open weak policies through config mistakes. | Added `SecurityHardening:Cors:StrictMode` and enforced explicit origin requirements in strict mode. |
| F-02 | Medium | `src/AuthServer/Program.cs` global limiter partition logic | Abuse controls existed, but sensitive endpoint families shared broad limits; token endpoint lacked tighter dedicated partition compared with admin/auth paths. | Hardened partitioning and lower limits for `/connect/token`; included explicit `/auth/login/mfa` path handling. |
| F-03 | Medium | `src/Api/Program.cs` CORS/security header pipeline | Api had good controls but no central hardening feature flags and no built-in rate limiting for admin/integration/hub paths. | Added `SecurityHardening` options, strict-mode CORS guard, and partitioned rate limiting for `/api/admin*`, `/api/integrations*`, `/hubs/app`. |
| F-04 | Low | `src/AppServer/Program.cs` middleware pipeline | AppServer did not set a baseline response header/caching hardening middleware like other services, raising consistency and operational assurance risk. | Added non-breaking baseline headers + no-store caching for `/app` and `/admin`, with production HSTS and hardening flag. |
| F-05 | Low | `src/AuthServer/Services/Diagnostics/ExceptionHandlingMiddleware.cs` redaction regex | Redaction covered key secrets but missed common key names (e.g., `api_key`, generic `token`) that may appear in exception data payloads. | Expanded redaction pattern to mask additional sensitive key names. |

## Quick wins
- Keep `SecurityHardening:*` enabled in production and only relax in development.
- Ensure credentialed CORS origin lists are explicit and environment-specific.
- Keep AuthServer and Api rate limiting enabled by default.
- Continue using placeholders/user-secrets for any secret-bearing settings.

## Hardening roadmap (next phase)
1. Add explicit per-endpoint `.RequireRateLimiting(...)` policies for auth/admin controllers to tighten controls by action class.
2. Add centralized structured log redaction pipeline for all services (not only exception persistence path).
3. Introduce CI security checks (`dotnet list package --vulnerable`, dependency/license gating, secret scanning).
4. Add CSP report endpoint with report-only rollout for UI-specific tuning.
5. Consider Redis/Hangfire production persistence hardening and retention/audit policy checks.

## Changes applied in this review
- `src/AuthServer/Options/SecurityHardeningOptions.cs`
- `src/AuthServer/Program.cs`
- `src/AuthServer/Services/Diagnostics/ExceptionHandlingMiddleware.cs`
- `src/AuthServer/appsettings.json`
- `src/AuthServer/appsettings.Development.json`
- `src/Api/Options/SecurityHardeningOptions.cs`
- `src/Api/Program.cs`
- `src/Api/appsettings.json`
- `src/Api/appsettings.Development.json`
- `src/AppServer/Security/SecurityHardeningOptions.cs`
- `src/AppServer/Program.cs`
- `src/AppServer/appsettings.json`
- `src/AppServer/appsettings.Development.json`
- `README.md` (new section added)
