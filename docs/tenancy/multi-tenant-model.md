# Multi-tenant Model

## Status: **Planned / Partial**

A full tenant-isolation model is **not fully implemented as a first-class platform contract**.
Current code shows tenant claim propagation patterns, but not a complete tenant-resolution + enforcement architecture across all services.

## What exists today
- Shared claim constant for tenant (`tenant`) in auth contracts.
- Api internal token minting copies incoming tenant claim when present.
- AppServer `CurrentUser` reads tenant claim from token context.
- SignalR supports tenant-scoped groups (`tenant:{tenantId}`) and Redis fanout can target tenant groups.

## What is missing for a complete multi-tenant model
- Canonical tenant resolution strategy (host/header/path) documented and uniformly enforced.
- Mandatory tenant claim issuance rules at AuthServer boundary.
- Cross-service persistence partitioning/authorization guarantees by tenant.
- End-to-end tenant boundary tests and operational controls.

## Recommended next implementation areas
1. Define resolver precedence (host/header/path) and codify middleware in AuthServer/Api.
2. Make tenant claim issuance explicit and mandatory for tenant-bound clients.
3. Enforce tenant-aware authorization in policy-map and AppServer resource handlers.
4. Add tenant-aware observability dimensions (logs/metrics/audit) and readiness checks.
