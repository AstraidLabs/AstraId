# Production Runbook

## Recommended topology
- Public edge:
  - AuthServer (OIDC + admin APIs as applicable)
  - Api (resource server and gateway)
- Internal-only:
  - AppServer
  - PostgreSQL
  - Redis

This matches the internal-token trust model: AppServer is intended for east-west calls from Api and should not be internet-exposed.

## TLS and reverse proxy assumptions
- AuthServer enforces HTTPS issuer outside Development.
- Use TLS termination at reverse proxy/load balancer or direct Kestrel TLS, but preserve HTTPS forwarding metadata.
- Keep secure cookies and HSTS enabled per security headers config.

## Data stores
- PostgreSQL: required for AuthServer Identity/OpenIddict and admin/governance persistence.
- Redis: required by Api/AppServer; AuthServer requires Redis outside Development.
- Hangfire:
  - AuthServer/AppServer currently configure in-memory storage in code.
  - For production resilience, move Hangfire storage to a durable backend before rollout.

## Data Protection key persistence
- AuthServer supports `AuthServer:DataProtection:KeyPath` and optional read-only mode.
- In production, persist keys on shared durable storage for stable cookie/token-protection behavior across restarts/replicas.

## Production configuration checklist
- [ ] `AuthServer:Issuer` set to public HTTPS URL.
- [ ] `ConnectionStrings:DefaultConnection` injected via secret source.
- [ ] Redis connection injected via secret source.
- [ ] CORS allow-lists explicitly configured (`Cors:AllowedOrigins`), no wildcard credentials mode.
- [ ] Security hardening enabled (`SecurityHardening` + `SecurityHeaders`) for AuthServer/Api/AppServer.
- [ ] Rate limiting enabled (AuthServer + Api).
- [ ] `InternalTokens` contract aligned between Api/AppServer (issuer, audience, JWKS URL, internal API key).
- [ ] AppServer mTLS enabled if required by environment boundary policy.
- [ ] No real secrets in `appsettings*.json`; use env variables or secret manager.
- [ ] Ops/admin endpoints restricted to admin policy.
