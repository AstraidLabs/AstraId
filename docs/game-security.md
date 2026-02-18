# Game Security Guide

## Authentication & token model

- Game API is hosted inside AppServer and keeps AstraID internal token enforcement.
- Web game client uses Authorization Code + PKCE (`oidc-client-ts`), not implicit/password grants.
- Access tokens are sent via `Authorization: Bearer`.

## Validation & abuse controls

- Command payloads are validated with FluentValidation.
- Command idempotency is enforced by unique `CommandId` index and dedupe checks.
- `/api/game/state` and `/api/game/commands` are rate limited with fixed-window policies.
- Command history (`GameCommand`) stores audit status/results.

## Hardening recommendations

- Keep tokens in memory/session storage, avoid long-lived localStorage tokens.
- Add strict CSP + trusted origins for client app deployment.
- Keep TLS-only, secure cookie options if future BFF/session integration is added.
