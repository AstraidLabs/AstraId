# Advanced OAuth Features

## Status legend
- **Implemented with policy controls**: feature is wired in runtime code.
- **Planned/partial**: feature has scaffolding but requires additional rollout policy or integration.

## Device Code flow
**Implemented with policy controls.**
- OpenIddict server enables device authorization flow and endpoints (`connect/device`, `connect/verify`).
- Runtime policy provider can disable/enable device flow without redeploying (`IOAuthAdvancedPolicyProvider` checks in OpenIddict event handlers).
- Config anchors: `AuthServer:DeviceFlow`, `AuthServer:OAuthAdvancedPolicyDefaults`.

## Token Exchange
**Implemented with policy controls.**
- OpenIddict custom grant type is enabled for token exchange (`TokenExchangeService.GrantType`).
- Runtime policy check can reject when disabled.
- Config anchors: `AuthServer:TokenExchange`, `AuthServer:OAuthAdvancedPolicyDefaults`.

## Refresh token rotation / reuse detection
**Implemented with policy controls.**
- Token defaults include rotation and reuse detection knobs.
- Services registered for reuse detection and remediation.
- Config anchors: `AuthServer:Tokens:RefreshPolicy` and `AuthServer:TokenPolicyDefaults`.

## Session management / logout channels
**Implemented/partial depending on deployment policy.**
- Session/back-channel services are registered.
- OpenIddict end-session endpoint is configured.
- Back/front-channel behavior is feature-configured via `AuthServer:SessionManagement` and advanced policy defaults.

## Required code areas for further hardening (planned improvements)
- Add comprehensive integration tests per advanced flow against deployed policy toggles.
- Add operator-facing runbooks for failure modes and rollback of policy changes.
- Expand admin diagnostics UX to show current effective policy and rollout history.
