# AuthServer Compatibility Contract

The API is configured to align with the AuthServer OpenIddict contract while preserving secure defaults.

## Configuration guardrails

- `Auth:Issuer`
  - Required outside `Development`.
  - Must be an absolute URI.
  - Must use `https` outside `Development`.
  - In `Development`, fallback remains `https://localhost:7001/`.
- `Auth:Audience`
  - Required outside `Development`.
  - In `Development`, fallback remains `api`.

## Validation mode

- API token validation uses OpenIddict discovery + JWKS (`UseSystemNetHttp`) with ASP.NET Core integration.
- No static signing key pinning is used.

## Claims/scopes contract

- Permission claim type is `permission` (`AuthConstants.ClaimTypes.Permission`).
- API expects `Auth:Scopes` (defaults to `api`) for Swagger OAuth scope wiring.

## Optional skew setting

- `Auth:ClockSkewSeconds` (optional integer)
  - When set to a value greater than `0`, it is applied to token validation clock skew.
  - When absent, default OpenIddict validation behavior is unchanged.

## Diagnostics endpoint

- `GET /api/integrations/authserver/contract`
  - Requires `RequireSystemAdmin` policy.
  - Returns a safe snapshot of the effective auth contract and policy-map refresh health, including last refresh/failure timestamps and refresh status.
  - Never includes secrets, API keys, or token contents.

- Swagger enablement flag and environment name are included for compatibility troubleshooting.
