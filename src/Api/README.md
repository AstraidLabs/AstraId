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

## Variant B internal token forwarding (API -> ContentServer)

- API validates end-user AuthServer tokens (issuer/signature/lifetime/audience) using `AddCompanyAuth`.
- API **does not** forward AuthServer bearer tokens to ContentServer.
- API issues an internal short-lived JWT (`InternalTokens`) and forwards that token downstream.
- Content operations are exposed through:
  - `GET /content/items` (`content.read` scope required)
  - `POST /content/items` (`content.write` scope required)

Required API config:

```json
{
  "InternalTokens": {
    "Issuer": "astraid-api",
    "Audience": "astraid-content",
    "LifetimeMinutes": 2,
    "SigningKey": "<set-via-env-or-user-secrets>"
  }
}
```
