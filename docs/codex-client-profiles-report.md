# Codex Client Profiles + Presets Report

## Phase 0 inventory (baseline before changes)

### Admin OIDC client CRUD map
- `AdminClientsController` exposes CRUD/toggle/rotate endpoints under `/admin/api/clients`. It already maps admin validation errors to `ValidationProblemDetails` with status 422.  
- `AdminClientService` is the primary orchestration layer for create/update/delete/list, OpenIddict descriptor mapping, client secret handling, and audit logging.  
- Existing validation rules in `AdminClientService.NormalizeAsync` + `OidcValidationSpec` included:
  - client ID syntax/length.
  - grant type normalization (`authorization_code`, `refresh_token`, `client_credentials`).
  - PKCE relation rules.
  - redirect URI format + HTTPS/loopback checks.
  - scope existence checks.  
- `ClientState` stored only `Enabled` state per application in DB.

### OIDC runtime map
- `/connect/authorize` and `/connect/token` are handled in `AuthorizationController`.
- Runtime currently checked enabled clients via `IClientStateService.IsClientEnabledAsync`.
- Token endpoint baseline allowed only `authorization_code` and `refresh_token` (before this change).
- Redirect URI and most protocol checks were delegated to OpenIddict defaults; custom profile/preset runtime enforcement did not exist.

### Gaps vs target design (baseline)
- No server-published profile rules API.
- No server-published preset catalog API.
- No profile/preset persistence per client.
- No runtime hard profile enforcement with explicit rule codes/audit.
- FE form used hardcoded validation/options and localized (non-English) helper texts in places.

## Implemented phases 1–6 (this change)

### Domain model
- Extended `ClientState` with profile/preset fields and overrides JSON.
- Added migration `AddClientPresetProfileStateFields`.
- Added static registries:
  - `ClientProfileRegistry` (hard constraints metadata).
  - `ClientPresetRegistry` (defaults/locked fields/allowed overrides + field metadata).
- Added `ClientConfigComposer` and `ClientConfigValidator` to compute/validate effective config server-side.

### Server as source of truth APIs
- Added:
  - `GET /admin/api/client-profiles/rules`
  - `GET /admin/api/client-presets`
  - `GET /admin/api/client-presets/{id}`

### Hard validation on save
- Create/update models now accept `profile`, `presetId`, `presetVersion`, `overrides`.
- Server validates preset existence/version/profile match and applies hard rules.
- Validation continues returning 422 `ValidationProblemDetails` with field keys.
- Added guard for seeded system-managed clients (`forceSystemManagedEdit`).

### Runtime enforcement
- Added `OidcClientPolicyEnforcer` used by `AuthorizationController` for authorize/token hard checks.
- Enforces redirect exact-match, grant restrictions, and PKCE requirements with OpenIddict error responses.
- Logs rule violations as token incidents with client/rule/path/trace context.

### Admin UI updates
- Client form now loads rules/presets from server.
- Create flow includes profile + preset selection and applies preset defaults.
- Save payload now sends profile/preset/version + overrides.
- Locked fields are disabled based on preset metadata.

### Governance and audit
- Existing audit logging for client lifecycle remains.
- Added profile/preset metadata persistence in client state.
- Runtime rule violations and secret operations are incident-logged.

## Usage notes
- Admin create/edit clients through `/admin/oidc/clients` form, selecting profile/preset first.
- Backend is authoritative: FE can provide overrides, server composes and validates effective config.
- OIDC requests violating profile rules fail at runtime even if invalid data was persisted previously.

## Optional future work
- Add richer per-field rule-code arrays in `ValidationProblemDetails.Extensions`.
- Add dedicated “Rules & Presets” read-only admin page.
- Add visual preset diff/re-apply workflow in edit mode.
