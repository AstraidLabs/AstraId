# Manual Verification Checklist

## A) Rotate signing key via admin UI
1. Open **Admin → Security → Signing Keys** and click **Rotate now**.
2. Confirm JWKS now publishes **two** keys (ACTIVE + PREVIOUS).
3. Issue a new token and verify its `kid` matches the ACTIVE key.

## B) Validate old tokens during grace period
1. Keep a token signed by the previous key.
2. Ensure it validates while the previous key is within the grace period.
3. After the grace period elapses, verify the previous key is retired and old tokens fail validation.

## C) Update token lifetimes
1. Open **Admin → Security → Token Policy** and change token lifetimes.
2. Issue new tokens and verify updated expiration values.

## D) Refresh token reuse detection
1. Attempt to reuse a refresh token after rotation.
2. Confirm a **refresh_token_reuse** incident is recorded.
3. Confirm user/client grants are revoked and a re-login is required.

## E) Disable rotation with break-glass
1. In production, attempt to disable rotation without break-glass.
2. Confirm the request is rejected.
3. Retry with `breakGlass=true` and a reason; verify a **critical** incident is logged.

