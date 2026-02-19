# IAM/OAuth inventory: Device Code, Token Exchange, Refresh Reuse Detection, Session Management

## Scope and sources

Inventory completed by inspecting local AuthServer source/config only:

- `src/AuthServer/Program.cs`
- `src/AuthServer/Controllers/AuthorizationController.cs`
- `src/AuthServer/Controllers/AuthController.cs`
- `src/AuthServer/Services/Tokens/RefreshTokenReuseDetectionService.cs`
- `src/AuthServer/Services/Tokens/RefreshTokenReuseRemediationService.cs`
- `src/AuthServer/Data/ApplicationDbContext.cs`
- `src/AuthServer/Services/Governance/TokenRevocationService.cs`
- `src/AuthServer/Options/*.cs`

## 1) Device Code Flow

### Implemented?
**No (missing).**

### Evidence
- OpenIddict server endpoints configured include authorization/token/introspection/userinfo/logout/revocation only.
- Allowed grants include authorization_code, refresh_token, client_credentials (and optional password).
- No device authorization endpoint URI configured.
- No device grant enabled.
- No verification/user_code endpoint/UI implementation found.

### Gaps
- Add `connect/device` and verification endpoint(s) in OpenIddict config.
- Enable device code grant flow.
- Add user interaction page to enter user code and approve/deny.
- Add rate limiting for polling/user_code entry and audit events.

## 2) Token Exchange (`urn:ietf:params:oauth:grant-type:token-exchange`)

### Implemented?
**No (missing).**

### Evidence
- Token endpoint currently rejects any grant type other than authorization_code/refresh_token/client_credentials/(optional password).
- No custom token exchange handler or grant-specific pipeline handling exists.
- No token exchange option section in `AuthServer:Features` or equivalent config.

### Gaps
- Add custom grant support in `/connect/token` pipeline.
- Add allowlists for requesting clients and target audiences/resources.
- Validate subject token and issue constrained delegated token.
- Add audit + incident logging (metadata only).

## 3) Refresh token reuse detection

### Implemented?
**Yes (present), with policy gating.**

### Evidence
- `RefreshTokenReuseDetectionService` tracks consumed refresh tokens by OpenIddict token id and detects reuse with configurable leeway.
- `AuthorizationController.Exchange()` invokes detection on refresh grant when policy enables rotation + reuse detection.
- On reuse, system logs token incident and calls `RefreshTokenReuseRemediationService`.
- Remediation revokes tokens/authorizations for subject (optionally per client) and rotates security stamp.
- Policy options/config include `RefreshRotationEnabled`, `RefreshReuseDetectionEnabled`, and leeway.

### Gaps
- Reuse detection exists; no mandatory implementation gap identified from inventory.
- Optional hardening: verify OpenIddict refresh one-time rotation behavior is always enforced in config.

## 4) Session management (back-channel logout / front-channel)

### Implemented?
**Partially implemented (local session/token revocation exists), back-channel/front-channel logout missing.**

### Evidence
- End-session endpoint (`connect/logout`) exists but only signs out local auth cookie + OIDC signout.
- Existing services can revoke tokens and authorizations for user/client (`TokenRevocationService`, `UserSessionRevocationService`).
- No tracking found for “clients participating in a given user session”.
- No back-channel logout token generation/delivery endpoint or client back-channel logout URL dispatch.
- No front-channel logout iframe orchestration found.

### Gaps
- Add session participation tracking.
- Add back-channel logout dispatch with signed logout token and per-client endpoint invocation.
- Gate via configuration flags and audit events.
- (Optional) front-channel only if existing partial support appears.

## Token persistence model inventory notes

- OpenIddict EF Core persistence is enabled via `options.UseOpenIddict()` in DbContext and `AddOpenIddict().AddCore().UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>()`.
- OpenIddict token/authorization entities are used for revocation flows (queried as `OpenIddictEntityFrameworkCoreToken` and `OpenIddictEntityFrameworkCoreAuthorization`).
- Explicit `UseReferenceRefreshTokens()`/`UseReferenceAccessTokens()` toggles were **not found** in current config.
- Existing reuse detection relies on refresh token private token identifier claim and a local `ConsumedRefreshTokens` table.
