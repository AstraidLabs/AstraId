# Codex Token Governance Report (AstraId)

## Phase 0 — Repository inventory (evidence-backed)

### OpenIddict server configuration
- OpenIddict server endpoints, flows, and JWKS endpoint are configured in `src/AuthServer/Program.cs` via `AddOpenIddict().AddServer(...)`, with authorization code + refresh token flow and JWKS endpoint configured. (`src/AuthServer/Program.cs`)
- Token policy configuration is applied through `OpenIddictTokenPolicyConfigurator` (authorization code, access token, identity token, refresh token lifetimes, and clock skew). (`src/AuthServer/Services/Tokens/OpenIddictTokenPolicyConfigurator.cs`)
- Signing credentials are configured via `OpenIddictSigningCredentialsConfigurator` which now supports certificate or DB key ring modes. (`src/AuthServer/Services/SigningKeys/OpenIddictSigningCredentialsConfigurator.cs`)

### /connect/token handling and supported grant types
- The token endpoint is implemented in `AuthorizationController.Exchange`, allowing `authorization_code` and `refresh_token` grants. (`src/AuthServer/Controllers/AuthorizationController.cs`)

### Token lifetimes and refresh token policies
- Configurable lifetimes are enforced by `TokenPolicyService`, `TokenPolicySnapshot`, and `TokenPolicyApplier` (access, identity, authorization code, and refresh). (`src/AuthServer/Services/Tokens/TokenPolicyService.cs`, `src/AuthServer/Services/Tokens/TokenPolicyApplier.cs`)
- Refresh token reuse detection is implemented in `RefreshTokenReuseDetectionService` with remediation via `RefreshTokenReuseRemediationService`, and enforcement in `AuthorizationController.Exchange`. (`src/AuthServer/Services/Tokens/RefreshTokenReuseDetectionService.cs`, `src/AuthServer/Services/Tokens/RefreshTokenReuseRemediationService.cs`, `src/AuthServer/Controllers/AuthorizationController.cs`)

### Signing key ring and rotation
- Signing keys are stored in `SigningKeyRingEntry` and persisted in the DB. (`src/AuthServer/Data/SigningKeyRingEntry.cs`)
- Key ring operations (create/rotate/revoke/retire, RSA generation, and public JWK storage) are in `SigningKeyRingService`. (`src/AuthServer/Services/SigningKeys/SigningKeyRingService.cs`)
- Rotation coordinator and background service enforce rotation schedules. (`src/AuthServer/Services/SigningKeys/SigningKeyRotationCoordinator.cs`, `src/AuthServer/Services/SigningKeys/SigningKeyRotationService.cs`)
- Key rotation policies are stored in `KeyRotationPolicy` with defaults seeded by `GovernancePolicyStore`. (`src/AuthServer/Data/KeyRotationPolicy.cs`, `src/AuthServer/Services/Governance/GovernancePolicyStore.cs`)

### Admin API + Admin UI (security governance)
- Signing keys admin API: list/rotate/retire/revoke and JWKS preview in `AdminSigningKeysController`. (`src/AuthServer/Controllers/Admin/AdminSigningKeysController.cs`)
- Rotation policy admin API: get/update in `AdminSecurityPoliciesController`. (`src/AuthServer/Controllers/Admin/AdminSecurityPoliciesController.cs`)
- Token policy admin API: get/update in `AdminSecurityPoliciesController` and `AdminTokensController`. (`src/AuthServer/Controllers/Admin/AdminSecurityPoliciesController.cs`, `src/AuthServer/Controllers/Admin/AdminTokensController.cs`)
- Incidents admin API: list/detail in `AdminTokenIncidentsController`. (`src/AuthServer/Controllers/Admin/AdminTokenIncidentsController.cs`)
- Admin UI pages for signing keys, rotation policy, token policy, and incidents exist in `src/Web/src/admin/pages/*` with navigation in `AppShell`. (`src/Web/src/admin/pages/SigningKeys.tsx`, `src/Web/src/admin/pages/SecurityRotationPolicy.tsx`, `src/Web/src/admin/pages/TokenPolicies.tsx`, `src/Web/src/admin/pages/SecurityIncidents.tsx`, `src/Web/src/admin/components/AppShell.tsx`)

### Persistence artifacts
- DB schema for signing keys, key rotation policy, token policy, token incidents, and consumed refresh tokens are tracked in EF migrations and the model snapshot. (`src/AuthServer/Migrations/*`, `src/AuthServer/Migrations/ApplicationDbContextModelSnapshot.cs`)

## Status table (required features)

| Feature | Status | Evidence |
| --- | --- | --- |
| Automatic signing key rotation + rollover (Active + Previous in JWKS; Active signing only) | **Implemented** | `SigningKeyRotationService`, `SigningKeyRotationCoordinator`, `SigningKeyRingService`, `OpenIddictSigningCredentialsConfigurator` |
| Token lifetimes configuration (access/id/code/refresh) | **Implemented** | `TokenPolicyService`, `OpenIddictTokenPolicyConfigurator`, `TokenPolicyApplier` |
| Refresh token rotation + reuse detection + remediation | **Implemented** | `AuthorizationController.Exchange`, `RefreshTokenReuseDetectionService`, `RefreshTokenReuseRemediationService` |
| Admin API for keys/rotation policy/token policy/incidents | **Implemented** | `AdminSigningKeysController`, `AdminSecurityPoliciesController`, `AdminTokensController`, `AdminTokenIncidentsController` |
| Admin UI for security sections | **Implemented** | `SigningKeys.tsx`, `SecurityRotationPolicy.tsx`, `TokenPolicies.tsx`, `SecurityIncidents.tsx` |
| DB-encrypted private key material | **Implemented** | `SigningKeyProtector`, `SigningKeyRingService` |
| Production/Development key mode switch (Certificates vs DbKeyRing) | **Implemented** | `AuthServerSigningKeyOptions`, `SigningKeyModeResolver`, `OpenIddictSigningCredentialsConfigurator` |

## Missing vs desired scope (before changes)
- JWKS safety margin for rollover was not configurable and the grace window did not account for token lifetimes.
- There was no admin JWKS preview endpoint/UI for public keys.
- No explicit mode switch between certificate-based signing and DB key ring signing.

## Risk assessment
- **Certificate mode requires signing certificate in production:** If production defaults to certificates without a configured signing certificate, startup will fail (by design). Ensure `AuthServer:Certificates:Signing` is configured or set `AuthServer:SigningKeys:Mode` to `DbKeyRing`.
- **JWKS caches:** JWKS cache safety margin should be tuned to match downstream caching behavior; insufficient margins can cause validation gaps during rollover.

## Dependency graph (high-level)
- `AuthServer/Program.cs`
  - OpenIddict server options
  - `OpenIddictSigningCredentialsConfigurator`
  - `OpenIddictTokenPolicyConfigurator`
  - Hosted services: `SigningKeyRotationService`, `GovernancePolicyInitializer`
- `SigningKeyRotationService` → `SigningKeyRotationCoordinator` → `SigningKeyRingService`
- `SigningKeyRotationCoordinator` → `TokenPolicyService` (safe rollover window)
- `AuthorizationController.Exchange` → `TokenPolicyService` + `RefreshTokenReuseDetectionService` + `RefreshTokenReuseRemediationService`
- Admin API controllers → Admin services → DB (governance + incidents)
- Admin SPA → `/admin/api/security/*` endpoints

## Runbook (operational)

### Enable DbKeyRing mode
1. Set `AuthServer:SigningKeys:Mode` to `DbKeyRing` (or leave `Auto` in Development).
2. Ensure Data Protection keys are configured for production (`AuthServer:DataProtection`), so private key material stays protected at rest.
3. Start the AuthServer; JWKS will publish active + previous keys, with Active used for signing.

### Configure rotation settings
1. Use Admin UI → **Security → Rotation Policy** to update:
   - Rotation interval (days)
   - Grace period (days)
   - JWKS cache margin (minutes)
2. Ensure the grace period is long enough to cover token lifetimes + cache margin.

### Emergency revoke
1. Admin UI → **Security → Signing Keys**.
2. Click **Revoke** on the compromised key.
3. A new active key is generated if the revoked key was active; JWKS updates immediately.

### Monitoring guidance
- Watch `TokenIncidents` for `refresh_token_reuse` and key rotation events.
- Track `SigningKeyRotationService` logs for rotation events and key cleanup.
- Verify JWKS endpoint (`/.well-known/jwks`) exposes Active + Previous keys during rotation.

## Change log (this implementation)
- Added key mode switching (Certificates vs DbKeyRing).
- Added JWKS cache margin to rotation policy and safe window calculation.
- Added JWKS preview endpoint and admin UI section.
