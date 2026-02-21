# IAM MVP Scope (As Implemented)

## Included in MVP
- OIDC authority endpoints via OpenIddict server setup:
  - discovery + JWKS
  - authorize, token, introspect, userinfo, revocation, logout
  - device and custom token-exchange endpoints are wired in server configuration
- Interactive account/auth flows through AuthServer controllers (`AuthController`, `AccountController`, `AuthorizationController`, `SelfServiceController`).
- Admin governance surfaces under `/admin/api/*` for:
  - users/roles/permissions
  - OIDC clients/scopes/resources
  - API resources + policy map management
  - token incidents, revocation, signing keys, security policies, privacy/inactivity/session settings
- Api resource server enforcement:
  - JWT/introspection/hybrid validation mode support
  - required scope + policy-map + permission checks
- Internal service identity propagation:
  - Api-issued internal JWTs for AppServer
  - AppServer service-token-only acceptance model

## Out-of-scope for MVP hard guarantees
- Fully productized multi-tenant boundary model (tenant claim propagation exists but full tenant-resolution/segregation strategy is not fully codified as a platform-wide contract).
- Full production ops plane beyond currently implemented `/health` and admin `/ops/health` snapshot surfaces.

## Primary code anchors
- AuthServer OpenIddict + grants + endpoints: `src/AuthServer/Program.cs`.
- Auth and consent flow: `src/AuthServer/Controllers/AuthorizationController.cs`.
- Admin APIs: controllers under `src/AuthServer/Controllers/Admin/`.
- Api authorization middleware + policy map: `src/Api/Middleware/EndpointAuthorizationMiddleware.cs`, `src/Api/Services/PolicyMapClient.cs`.
- Internal token mint/validate chain: `src/Api/Security/InternalTokenService.cs`, `src/AppServer/Program.cs`.
