# Security Hardening Phase 1

## Summary of findings and applied changes

This phase implemented minimal, targeted hardening updates across AuthServer, Api, and AppServer with production-safe defaults and development-friendly fallbacks.

Applied themes:
- removed tracked secret values from committed configuration
- tightened AuthServer CORS credentials behavior through explicit origin allowlists
- added server-side rate limiting for sensitive AuthServer paths
- moved anti-abuse state to distributed cache with memory fallback in development
- gated Hangfire dashboards to prevent public exposure
- redacted sensitive values in exception persistence and reduced production exception detail by default
- tightened AppServer internal JWT validation boundaries
- reduced downstream internal token scopes to endpoint policy-derived least privilege
- introduced configurable security headers baseline

---

## Before/after notes

### CORS (AuthServer)
- **Before:** static policy used `SetIsOriginAllowed(_ => true)` with `AllowCredentials()`.
- **After:** static policy uses `WithOrigins(...)` from `Cors:AllowedOrigins`; credentials are only enabled when configured and origins are present. `*` is blocked by startup validation. Dynamic `ClientCorsPolicyProvider` remains active and aligned with the same config source.

### Secrets in tracked config
- **Before:** concrete password values existed in tracked development config.
- **After:** replaced with `__REPLACE_ME__` placeholders, retained structure, and documented `user-secrets`/environment setup.

### Rate limiting (AuthServer)
- **Before:** no global middleware policy for sensitive endpoints.
- **After:** path-aware global limiter with strict limits for `/admin/api/*` and auth/token/introspection/revocation flows; 429 response and safe logging.

### Redis anti-abuse state
- **Before:** `AuthRateLimiter` and `MfaChallengeStore` used in-memory cache only.
- **After:** both prefer `IDistributedCache` (Redis when configured), with fallback to memory cache in development or redis-failure cases.

### Dashboards
- **Before:** Hangfire dashboard local-only filter under `/hangfire`.
- **After:**
  - AuthServer dashboard moved to `/admin/hangfire` and protected by admin-aware filter (development loopback fallback).
  - AppServer dashboard moved to `/admin/hangfire` and constrained to loopback (and authenticated outside development).

### Exception redaction
- **Before:** exception stack/data persisted without sensitive-token redaction and with full detail in production.
- **After:** redaction for authorization/token/password-related patterns and minimal production detail unless explicitly enabled via diagnostics option.

### Internal token boundary
- **Before:** AppServer validated signing key/issuer/audience/lifetime but with looser algorithm/claim checks; Api derived downstream scope by HTTP method.
- **After:**
  - AppServer enforces HS256-only internal tokens, requires `iat/nbf/exp`, and preserves explicit rejection of AuthServer-issued JWTs.
  - Api now derives internal token scopes from endpoint authorization metadata (`RequireContentRead`/`RequireContentWrite`) instead of HTTP method.

---

## Security headers baseline

Enabled via `SecurityHeaders` options in AuthServer and Api.

Baseline headers:
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: no-referrer`
- `Permissions-Policy` with common features disabled
- `Cross-Origin-Opener-Policy: same-origin`
- `Cross-Origin-Resource-Policy: same-site`
- HSTS in production when `SecurityHeaders:EnableHsts=true`
- CSP mode configurable (`Off`, `ReportOnly`, `Enforce`)

Path-sensitive cache protection:
- AuthServer: `/connect/*` and `/auth/*` responses get `Cache-Control: no-store`.
- Api: `/api/*` and `/app/*` responses get `Cache-Control: no-store`.

Tuning guidance:
- adjust `AllowedFrameAncestors` as needed for embedded admin usage
- use `CspMode=ReportOnly` during rollout
- avoid wildcard frame ancestors in production (startup validation blocks unsafe wildcard)

---

## File-by-file modifications

- `src/AuthServer/Program.cs`
- `src/AuthServer/Services/Cors/ClientCorsPolicyProvider.cs`
- `src/AuthServer/Services/AuthRateLimiter.cs`
- `src/AuthServer/Services/MfaChallengeStore.cs`
- `src/AuthServer/Services/Diagnostics/ExceptionHandlingMiddleware.cs`
- `src/AuthServer/Services/Events/InMemoryEventPublisher.cs`
- `src/AuthServer/Seeding/AuthBootstrapHostedService.cs`
- `src/AuthServer/Services/Jobs/SecureDashboardAuthorizationFilter.cs`
- `src/AuthServer/Options/CorsOptions.cs`
- `src/AuthServer/Options/SecurityHeadersOptions.cs`
- `src/AuthServer/Options/DiagnosticsOptions.cs`
- `src/AuthServer/AuthServer.csproj`
- `src/AuthServer/appsettings.json`
- `src/AuthServer/appsettings.Development.json`
- `src/Api/Program.cs`
- `src/Api/Integrations/InternalTokenHandler.cs`
- `src/Api/Security/InternalTokenService.cs`
- `src/Api/Options/InternalTokenOptions.cs`
- `src/Api/Options/SecurityHeadersOptions.cs`
- `src/Api/appsettings.json`
- `src/Api/appsettings.Development.json`
- `src/AppServer/Program.cs`
- `src/AppServer/Security/InternalTokenOptions.cs`
- `src/AppServer/Infrastructure/Hangfire/SecureDashboardAuthorizationFilter.cs`
- `src/AppServer/appsettings.json`
- `src/AppServer/appsettings.Development.json`
- `.gitignore`
- `README.md`
- `docs/security/local-secrets.md`

---

## Manual follow-ups (env vars, user-secrets, infra)

1. Set all secret values via user-secrets/environment variables before running locally.
2. Configure production `Cors:AllowedOrigins` to explicit trusted UI origins only.
3. Ensure Redis is enabled in non-development deployments (`Redis:ConnectionString`).
4. Confirm reverse proxy/network policy does not expose `/admin/hangfire` publicly.
5. Validate CSP and frame ancestor settings with actual admin UI requirements.
6. Keep `Diagnostics:StoreDetailedExceptionDataInProduction=false` unless incident debugging requires temporary opt-in.
