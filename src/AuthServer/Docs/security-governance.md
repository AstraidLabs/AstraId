# Security Governance Design

## A) Runtime policy via DB
- **KeyRotationPolicy** and **TokenPolicy** are stored in the database and loaded at runtime.
- Appsettings provide defaults (`AuthServer:KeyRotationDefaults`, `AuthServer:TokenPolicyDefaults`) and guardrails (`AuthServer:GovernanceGuardrails`).
- On startup, `GovernancePolicyInitializer` ensures policy rows exist and clamps defaults to guardrails.

## B) Signing keys lifecycle
- **Lifecycle states:** ACTIVE → PREVIOUS → RETIRED, with REVOKED for emergency removal.
- **Rotation interval:** `KeyRotationPolicy.RotationIntervalDays`.
- **Grace period:** `KeyRotationPolicy.GracePeriodDays` keeps previous keys published for validation.
- **Emergency rotation:** Admin “rotate now” calls the coordinator, forcing rotation regardless of schedule.

## C) JWKS behavior
- JWKS publishes **ACTIVE + PREVIOUS** keys (public material only).
- New tokens are signed **only** with ACTIVE (Active signing credential is registered first).

## D) Refresh token policy
- **Rotation enabled:** via `TokenPolicy.RefreshRotationEnabled`.
- **Reuse detection:** via `TokenPolicy.RefreshReuseDetectionEnabled` and `RefreshReuseLeewaySeconds`.
- **On reuse detection:**
  - Create a `TokenIncident` with severity `high`.
  - Revoke grants/tokens for the subject + client.
  - Update user security stamp (forces re-authentication).

## E) Admin UX + warnings
- Admin UI shows warnings:
  - Rotation disabled.
  - Only one key published.
  - Active key expiring soon.
  - Refresh reuse detection disabled.
- Disabling rotation in production requires break-glass confirmation and logs a `critical` incident.
- Inputs are validated using guardrails (min/max) both on startup and during admin updates.
- Client secret and API key rotations are audited and logged as incidents; secret material is never returned to the admin UI.

## F) Multi-instance safety
- Rotation uses a **row lock** on `KeyRotationPolicies` inside a transaction (`SELECT ... FOR UPDATE`).
- Ensures a single ACTIVE key and serialized rotations across instances.

## G) Related subsystem governance
- **Encryption keys:** Admin UI exposes certificate metadata (thumbprint, validity, source) without private material.
- **DataProtection:** Admin UI shows persistence location, key count, and read-only mode.
- **Client secrets & API keys:** Rotation actions are audited and recorded as incidents; secret values are never returned in responses.
- **Revocation tools:** Admin endpoints revoke user/client grants and invalidate sessions via security stamp updates.
