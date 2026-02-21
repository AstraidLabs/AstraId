# Platform Checks

## Current implementation status
**Implemented (Api).**

- Public liveness endpoint: `GET /health` (anonymous).
- Admin-protected ops endpoint group: `/ops/*` with `RequireSystemAdmin` policy.
- `GET /ops/health` returns cached platform snapshot data when `OpsEndpoints:Enabled=true`.
- Snapshot data is maintained by `PlatformHealthSnapshotCache` hosted service with configurable interval (`OpsEndpoints:CheckIntervalSeconds`).

## Design notes
- Intended audience is admin/internal operators; non-admin callers should not use `/ops/*`.
- If ops checks are disabled in config, endpoint returns service unavailable problem details.
- Service checks reference downstream services configured in `Services:*` (AuthServer/Cms/AppServer), plus policy-map readiness and dependency status data.

## Admin UI surface
- README states the admin UI includes Platform Health and consumes Api `/ops/health`.
- Keep this surface behind admin auth and avoid exposing internal diagnostics publicly.

## Operational usage
1. Verify admin token has `system.admin` permission.
2. Call `GET /ops/health`.
3. Confirm policy map and dependency statuses are healthy before deploy promotion.
4. Treat stale/failed snapshot status as release gate failure.
