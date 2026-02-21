# Health and Readiness

## Endpoint model
- `GET /health`:
  - Implemented in Api (anonymous) and AppServer (anonymous simple OK payload).
  - Suitable for container/platform liveness probes.
- `GET /ops/health` (Api):
  - Admin-only readiness/diagnostic surface.
  - Returns platform snapshot details when enabled.

## Readiness gates to enforce
1. AuthServer reachable and serving OIDC discovery endpoint.
2. Api policy-map refresh status is healthy (`ok`) and not stale.
3. AppServer health endpoint reachable from Api service network path.
4. Redis and required downstream dependencies healthy for each service.
5. Internal token JWKS path available to AppServer (`/internal/.well-known/jwks.json` on Api) with aligned internal API key.

## Policy-map readiness
- Api denies unknown `/api/*` routes by design if policy map lacks matching entry.
- Deployment readiness should verify policy map refresh succeeded after release.

## Suggested probe split
- Liveness: `/health` for each service.
- Readiness: admin-authenticated `/ops/health` plus targeted synthetic checks for token and policy-map paths.
