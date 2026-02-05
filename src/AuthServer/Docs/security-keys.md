# Security keys and token policy

## Signing key rotation
AuthServer maintains a signing key ring for JWT/OIDC signing. The ring always has one **ACTIVE** key (used for new tokens) and may have one **PREVIOUS** key (kept in JWKS to validate tokens issued before the last rotation). Retired or revoked keys are never published. The signing key ring is stored in the database with encrypted private key material. 

Rotation is handled by a hosted service:

- If no active key exists, a new active key is generated immediately.
- When the active key reaches `AuthServer:SigningKeys:RotationIntervalDays`, it is demoted to **PREVIOUS** and a fresh **ACTIVE** key is generated.
- Previous keys are retained until `PreviousKeyRetentionDays` elapses, then marked **RETIRED** and removed from JWKS.

Manual rotation/retirement/revocation is available for admins via `/admin/api/security/keys/signing`.

## JWKS publication
JWKS (`/.well-known/jwks`) exposes **ACTIVE** and **PREVIOUS** keys only. Tokens are always signed with the active key, so the `kid` in newly issued JWTs matches the current active key.

## Token lifetimes and refresh protection
Token lifetimes and refresh-token safeguards are controlled via `AuthServer:Tokens` and optional overrides stored in the database. Admins can view and update policies via `/admin/api/security/tokens/policy`.

Refresh token reuse detection is enabled by default. If a refresh token is reused, AuthServer revokes the subject’s tokens/authorizations for the client and invalidates the user’s session by updating the security stamp.
