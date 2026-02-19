# AuthServer Logging & Diagnostics Inventory

## Current implementation inventory

### Exception handling + DB-backed diagnostics
- `ExceptionHandlingMiddleware` is registered globally (`app.UseMiddleware<ExceptionHandlingMiddleware>()`) and handles unhandled exceptions before writing problem details to clients. It writes an error log record into `ApplicationDbContext.ErrorLogs` when `Diagnostics:StoreErrorLogs=true`. (`src/AuthServer/Program.cs`, `src/AuthServer/Services/Diagnostics/ExceptionHandlingMiddleware.cs`)
- `ExceptionHandlingMiddleware` already performs basic regex redaction for some key/value secrets in stack traces, exception data, and user-agent before persistence. (`src/AuthServer/Services/Diagnostics/ExceptionHandlingMiddleware.cs`)
- `UseStatusCodePages` in `Program.cs` captures non-success status pages and stores server-side 5xx records via `StoreStatusCodeErrorAsync`, also persisted into `ErrorLogs`. (`src/AuthServer/Program.cs`)
- Error log retention/cleanup exists via `ErrorLogCleanupService` based on `Diagnostics:MaxStoredDays`. (`src/AuthServer/Services/Diagnostics/ErrorLogCleanupService.cs`, `src/AuthServer/Options/DiagnosticsOptions.cs`)

### Diagnostics endpoints + admin UI paths
- Error diagnostics API endpoints:
  - `GET /admin/api/diagnostics/errors`
  - `GET /admin/api/diagnostics/errors/{id}`
  both protected by `AdminOnly` policy and backed by `ErrorLogs` data. (`src/AuthServer/Controllers/Admin/AdminDiagnosticsController.cs`)
- Token incident diagnostics endpoint:
  - `GET /admin/api/security/token-incidents`
  - `GET /admin/api/security/token-incidents/{id}`
  backed by `TokenIncidents` table. (`src/AuthServer/Controllers/Admin/AdminTokenIncidentsController.cs`)
- Audit endpoint:
  - `GET /admin/api/audit`
  backed by `AuditLogs` table. (`src/AuthServer/Controllers/Admin/AdminAuditController.cs`)
- Admin SPA path fallback is mapped under `/admin/{*path:nonfile}` with static UI assets. (`src/AuthServer/Program.cs`)

### Security event logging already present
- Security/login history persistence exists in `LoginHistoryService` (`LoginHistory` table), storing success/failure, reason code, user/client metadata, ip, user-agent, trace id. (`src/AuthServer/Services/Security/LoginHistoryService.cs`)
- Token incidents are persisted through `TokenIncidentService` (`TokenIncidents` table). (`src/AuthServer/Services/Governance/TokenIncidentService.cs`)
- Administrative/governance actions write to `AuditLogs` from multiple services/controllers (client/user/role/scope/resource/policy/revocation/introspection/security lifecycle actions). (`src/AuthServer/Services/Admin/*`, `src/AuthServer/Controllers/Admin/*`, `src/AuthServer/Services/Security/*`, `src/AuthServer/Services/OpenIddict/OpenIddictIntrospectionHandlers.cs`)
- Rate limiting is enabled with route partitions and emits warning log entries on rejections through logger category `RateLimiter`. (`src/AuthServer/Program.cs`)

### Correlation handling
- `CorrelationIdMiddleware` ensures `X-Correlation-ID` exists (generated if missing), stores it in `HttpContext.TraceIdentifier`, writes it to response header, and opens a logger scope containing `CorrelationId`. (`src/AuthServer/Services/CorrelationIdMiddleware.cs`)
- Middleware is registered in pipeline (`app.UseMiddleware<CorrelationIdMiddleware>()`). (`src/AuthServer/Program.cs`)

### Logging providers/sinks/config
- Current logging config relies on built-in `Logging:LogLevel` in appsettings (console/default providers from ASP.NET host, no custom sink split by audience). (`src/AuthServer/appsettings.json`, `src/AuthServer/appsettings.Development.json`)
- `DiagnosticsOptions` controls diagnostics behavior (admin exposure, DB persistence, retention days, detailed exception data in production). (`src/AuthServer/Options/DiagnosticsOptions.cs`)

## Gap analysis vs target requirements

### What is already aligned
- Global exception middleware and DB-backed error diagnostics exist.
- Admin diagnostics/audit endpoints and token-incident tracking already exist.
- Correlation ID middleware already exists.
- Basic security event records (login history, incidents, audit logs) already exist.

### Missing or incomplete
1. **Centralized redaction utility**
   - Redaction logic is local to exception middleware and regex-only.
   - No shared sanitizer for headers/query/exception strings across all logging points.
   - Status code diagnostics persistence and other services may store unsanitized path/query/UA/details.

2. **Explicit runtime mode model (Development vs Production) for logging**
   - No unified `AstraLogging` options contract with mode, stream toggles, min levels, request logging policy.

3. **Three explicit logging streams/audiences**
   - No dedicated categories/options abstraction for:
     - application (ops/admin runtime)
     - developer diagnostics stream (gated in prod)
     - security audit stream

4. **Structured security audit logger abstraction**
   - Existing `AuditLog` DB writes are domain-action audit records but there is no cross-cutting `ISecurityAuditLogger` model with standardized fields (actorType/result/reasonCode/correlation/trace/ip/userAgentHash/service/environment).

5. **Request logging policy + redaction behavior by mode**
   - No dedicated request logging middleware with production-safe defaults (no body, strict redaction, query off by default).

6. **Consistent cross-service enrichment**
   - Correlation is present in AuthServer only; Api/AppServer do not currently enforce/propagate standardized inbound correlation middleware and trace enrichment.

7. **Retention taxonomy for new streams**
   - Existing DB retention applies to `ErrorLogs` cleanup; no explicit retention/config model for stream-specific sinks (especially security audit stream).

## Minimal additive plan for AuthServer
- Keep existing diagnostics DB tables and admin endpoints unchanged.
- Add shared `AstraLogging` options + centralized sanitizer.
- Integrate sanitizer into exception middleware and status-code diagnostics persistence.
- Add request logging middleware with mode-aware behavior and strict denylist defaults.
- Add standardized `ISecurityAuditLogger` implementation that writes structured events to dedicated audit logger category (plus optional DB persistence for event stream if enabled).
- Keep existing domain audits/login history/incidents; add cross-cutting security audit events only where missing and without altering auth/business outcomes.
