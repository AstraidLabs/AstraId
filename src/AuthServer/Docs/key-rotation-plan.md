# Key & Token Governance — Current State vs Required

## Current State (Evidence)

### OpenIddict server configuration
- Issuer, endpoints, flows, scopes, and token encryption are configured in `Program.cs` using OpenIddict server options, including authorization code + refresh token flow and JWKS endpoint. (`src/AuthServer/Program.cs`)
- Encryption certificate configuration uses `CertificateLoader` and falls back to development encryption certs in development. (`src/AuthServer/Program.cs`, `src/AuthServer/Services/Cryptography/CertificateLoader.cs`)

### Signing key storage + rotation
- Signing keys are stored in `SigningKeyRingEntries` with statuses Active/Previous/Retired/Revoked. (`src/AuthServer/Data/SigningKeyRingEntry.cs`)
- Rotation and cleanup are implemented in `SigningKeyRingService` and a background rotation service that rotates on an interval and publishes active/previous keys for JWKS. (`src/AuthServer/Services/SigningKeys/SigningKeyRingService.cs`, `src/AuthServer/Services/SigningKeys/SigningKeyRotationService.cs`)
- OpenIddict signing credentials are loaded from the DB at startup and refreshed on rotation. (`src/AuthServer/Services/SigningKeys/OpenIddictSigningCredentialsConfigurator.cs`)

### Token lifetimes + refresh handling
- Token lifetimes are applied in `AuthorizationController` via `TokenPolicyApplier`, including access, identity, and refresh tokens. (`src/AuthServer/Controllers/AuthorizationController.cs`, `src/AuthServer/Services/Tokens/TokenPolicyApplier.cs`)
- Refresh token reuse detection exists using a consumed refresh token table and remediation that revokes grants for the subject/client. (`src/AuthServer/Services/Tokens/RefreshTokenReuseDetectionService.cs`, `src/AuthServer/Services/Tokens/RefreshTokenReuseRemediationService.cs`)

### Admin APIs + UI
- Admin API endpoints exist for signing keys and token policies and are surfaced in the admin UI. (`src/AuthServer/Controllers/Admin/AdminSigningKeysController.cs`, `src/AuthServer/Controllers/Admin/AdminTokensController.cs`, `src/Web/src/admin/pages/SigningKeys.tsx`, `src/Web/src/admin/pages/TokenPolicies.tsx`)
- Audit logging for admin actions exists. (`src/AuthServer/Data/AuditLog.cs`, `src/AuthServer/Controllers/Admin/AdminAuditController.cs`)
- Error logging and diagnostics endpoints exist. (`src/AuthServer/Data/ErrorLog.cs`, `src/AuthServer/Controllers/Admin/AdminDiagnosticsController.cs`)

### EF Core models
- Existing tables include `SigningKeyRingEntries`, `TokenPolicyOverrides`, `ConsumedRefreshTokens`, `AuditLogs`, and `ErrorLogs`. (`src/AuthServer/Data/ApplicationDbContext.cs`, `src/AuthServer/Data/TokenPolicyOverride.cs`, `src/AuthServer/Data/ConsumedRefreshToken.cs`)

## Required vs Current Gaps

| Requirement | Current State | Gap / Action |
| --- | --- | --- |
| DB-backed key rotation policy | Options-based rotation interval + retention | Add `KeyRotationPolicy` table + admin control with guardrails; use DB for rotation interval/grace |
| Token policy in DB | Token policy overrides exist but no explicit auth code/clock skew fields | Add `TokenPolicy` table with explicit lifetimes + reuse policy; remove reliance on overrides |
| Key rotation incidents + auditing | Audit logs exist, incidents do not | Add `TokenIncident` table + log key rotations/revocations/policy changes |
| Admin security governance UI | Signing keys + token policy only | Add Security section tabs for rotation policy, incidents, revocation, DataProtection |
| DataProtection key persistence visibility | No admin status | Add DataProtection status API + UI |
| Encryption key metadata | No admin status | Add encryption key status (metadata only) |
| Revocation tools | Only implicit refresh reuse remediation | Add admin endpoints to revoke user/client grants |
| Multi-instance safety | None for rotation | Add DB row lock for rotation jobs + admin rotate |

## Implementation Plan (This Change Set)
- Introduce DB-backed policies (`KeyRotationPolicy`, `TokenPolicy`) and a policy initializer with guardrails and defaults.
- Introduce `TokenIncident` records and logging for key rotations, revocations, refresh reuse, and policy changes.
- Update signing key rotation to use DB policy and transaction row locks for multi-instance safety.
- Update OpenIddict configuration to apply token policy defaults (authorization code lifetime, clock skew, etc.).
- Add admin endpoints + UI for rotation policy, token policy, incidents, revocation, DataProtection status, and encryption certificate metadata.

