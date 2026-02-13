# Password Grant Evidence Report

## AuthServer runtime and OpenIddict wiring

- `src/AuthServer/Program.cs:284-291` — OpenIddict server flow registration now conditionally calls `AllowPasswordFlow()` based on `AuthServer:Features:EnablePasswordGrant` (config-driven).
- `src/AuthServer/Controllers/AuthorizationController.cs:240-254` — token endpoint checks grant type and rejects password grant with `unsupported_grant_type` when feature is disabled (runtime enforcement, server-authoritative).
- `src/AuthServer/Controllers/AuthorizationController.cs:388-390` — password-handler level guard also blocks password grant when disabled (runtime enforcement).
- `src/AuthServer/Services/OidcClientPolicyEnforcer.cs:66-80` — when password grant is used, enforces confidential + integration + allowUserCredentials + scope restrictions (runtime policy gate).

## Admin/API configuration and validation

- `src/AuthServer/Services/Admin/ClientConfigValidator.cs:89-94` — rejects saving clients with password grant when feature flag is disabled (config-driven behavior).
- `src/AuthServer/Controllers/Admin/AdminFeaturesController.cs:1-27` — exposes `GET /admin/api/features` with `enablePasswordGrant` for UI gating (config-driven readout).
- `src/AuthServer/Services/Admin/ClientProfiles.cs:80-106` — presets are code/refresh/client-credentials oriented; password grant is not preset-default (hard-coded safe defaults).

## Seeding/definitions

- `src/AuthServer/Seeding/AuthServerDefinitions.cs:67-114` — seeded clients (`web-spa`, `resource-api`) do not include password grant by default.
- `src/AuthServer/Seeding/AuthBootstrapHostedService.cs:203-219` — seeding permission mapper supports password permission only if a client definition includes password grant; current seeds do not include it.

## Admin UI and client-side selectors

- `src/Web/src/admin/pages/ClientForm.tsx:652-677` — password grant option rendered only when feature endpoint reports enabled; shown under advanced/legacy warning UI.
- `src/Web/src/admin/pages/ClientForm.tsx:725-734` — `allowUserCredentials` checkbox is disabled when password grant feature is off.
- `src/Web/src/admin/validation/oidcValidation.ts:10` — validation still recognizes `password` as known grant type for compatibility with existing clients (client-side only; server remains authoritative).

## Configuration defaults (Development + Production)

- `src/AuthServer/Options/AuthServerAuthFeaturesOptions.cs:3-8` — default option value is `EnablePasswordGrant = false`.
- `src/AuthServer/appsettings.json:79-81` — production/default config sets `EnablePasswordGrant` to `false`.
- `src/AuthServer/appsettings.Development.json:79-81` — development config also sets `EnablePasswordGrant` to `false`.

## Documentation references

- `README.md:45-46, 85-107` — docs describe supported default flows and password grant as explicit legacy opt-in only.

