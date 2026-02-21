# Security Hardening

## Implemented controls (code-backed)
- **AuthServer hardening options**
  - Validates CORS allow-list safety and production constraints.
  - Post-configures production defaults to force hardening features on.
- **Api hardening options**
  - Applies security headers middleware, CORS policy, and rate limiting.
  - Uses endpoint-level authorization middleware for `/api/*` with required scope + policy-map + permission checks.
- **AppServer hardening options**
  - Applies security headers middleware.
  - Requires internal-token validation with strict issuer/audience/service checks.
  - Supports mTLS requirement and allow-lists at Kestrel + request middleware levels.

## Rate limiting
- AuthServer: rate limiter configured with dedicated limits for auth-sensitive routes (login, token and related endpoints).
- Api: rate limiter partitions sensitive surfaces (e.g., `/api/admin`, `/api/integrations`, `/hubs/app`) with audit logging on rejections.

## CORS
- AuthServer and Api read explicit `Cors:AllowedOrigins` and disallow unsafe wildcard patterns in hardened scenarios.
- For production, keep strict allow-list and avoid broad origins.

## Admin protections
- AuthServer admin controllers use `Authorize(Policy = "AdminOnly")`.
- Api ops endpoint group (`/ops/*`) requires `RequireSystemAdmin` policy.
- AppServer Hangfire dashboard uses custom authorization filter and is constrained for secure use.

## Internal boundary and mTLS
- Api issues short-lived internal tokens for AppServer calls.
- AppServer accepts API-issued internal tokens only and rejects AuthServer user tokens.
- Api supports outbound AppServer mTLS configuration.
- AppServer can require client certificates and validate thumbprint/subject allow-lists.

## Logging and redaction
- `AstraLogging` is configured in all services.
- `RedactionEnabled` is present in configuration.
- Request logging includes body/query controls and block-list prefixes for sensitive route families.

## Recommended defaults
- Keep `SecurityHardening:Enabled=true` in all production services.
- Keep `SecurityHeaders:EnableHsts=true` in production.
- Keep Api/AuthServer rate limiting enabled.
- Keep request-body logging disabled for auth/admin routes.
- Keep AppServer non-public and enforce mTLS where platform policy requires it.
