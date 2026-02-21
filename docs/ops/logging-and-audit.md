# Logging and Audit

## Implemented status
**Implemented across AuthServer, Api, AppServer via AstraLogging.**

## Streams and intent
- Application stream: operational logs.
- Developer diagnostics stream: enabled in Development by default pattern.
- Security audit stream: security decisions/events (auth failures, rate-limit rejections, token and mTLS failures).
- Request logging stream: request metadata with configurable query/body inclusion.

## Redaction and sensitive-route controls
- `AstraLogging:RedactionEnabled` is present in service configs.
- Request logging defaults include controls for query/body capture and max body length.
- Sensitive route prefixes are block-listed in config (e.g., `/connect/`, `/auth/`, `/admin/`, `/account/`, `/hubs/`).

## Audit event examples from code
- Api rate-limit rejection emits `api.rate_limit.rejected` security audit event.
- Api authorization middleware emits events for unauthorized, missing scope, missing permission, and policy-map denials.
- AppServer emits audit events for invalid internal token format, issuer/audience failures, service allow-list failures, and mTLS failures.

## Operational guidance
- Keep developer diagnostics off in production unless incident response requires temporary elevation.
- Keep request body logging disabled for auth/admin surfaces.
- Forward security audit stream to centralized SIEM with retention policy.
