# User deletion lifecycle security report

## Repository scan: tables referencing `AspNetUsers`

### Identity and auth-core references
- `AspNetUserClaims.UserId` via `FK_AspNetUserClaims_AspNetUsers_UserId` (`CASCADE`): **CASCADE acceptable** because claims are identity dependents and must be removed in hard delete.
- `AspNetUserLogins.UserId` via `FK_AspNetUserLogins_AspNetUsers_UserId` (`CASCADE`): **CASCADE acceptable**.
- `AspNetUserRoles.UserId` via `FK_AspNetUserRoles_AspNetUsers_UserId` (`CASCADE`): **CASCADE acceptable**.
- `AspNetUserTokens.UserId` via `FK_AspNetUserTokens_AspNetUsers_UserId` (`CASCADE`): **CASCADE acceptable**.
- `UserActivities.UserId` via `FK_UserActivities_AspNetUsers_UserId` (`CASCADE`): **CASCADE acceptable** for activity telemetry.

### History/audit references
- `AuditLogs.ActorUserId` via `FK_AuditLogs_AspNetUsers_ActorUserId` (`SET NULL`): **SET NULL required** to preserve audit trail.
- `ErrorLogs.ActorUserId` via `FK_ErrorLogs_AspNetUsers_ActorUserId` (`SET NULL`): **SET NULL required**.
- `TokenIncidents.ActorUserId` via `FK_TokenIncidents_AspNetUsers_ActorUserId` (`SET NULL`): **SET NULL required**.
- `TokenIncidents.UserId` via `FK_TokenIncidents_AspNetUsers_UserId` (`SET NULL`): **SET NULL recommended** to keep incident history.
- `UserSecurityEvents.UserId` via `FK_UserSecurityEvents_AspNetUsers_UserId` (`SET NULL`): **SET NULL recommended**.
- `UserLifecyclePolicies.UpdatedByUserId` via `FK_UserLifecyclePolicies_AspNetUsers_UpdatedByUserId` (`SET NULL`): **SET NULL recommended**.

### OpenIddict note
OpenIddict token/authorization records are keyed by `Subject` string and are revoked explicitly in lifecycle service. This avoids blind FK cascades and supports deterministic revocation before anonymization/hard delete.

## PII classification and anonymization scope
PII fields removed/replaced during anonymization:
- `AspNetUsers.Email`, `NormalizedEmail`, `UserName`, `NormalizedUserName`, `PhoneNumber`, `PasswordHash`.
- Security state reset: `EmailConfirmed=false`, `TwoFactorEnabled=false`, lockout applied.
- Identity auxiliary PII rows removed: user claims/logins/tokens.

Deterministic replacement:
- `UserName = deleted_{userId:N}`
- `Email = deleted_{userId:N}@deleted.local`

## Retention/audit requirements
Must be preserved for security/audit:
- `AuditLogs`
- `ErrorLogs`
- `TokenIncidents`
- `UserSecurityEvents`

These now keep records even after hard deletion because actor/user references are nullable and configured to `SET NULL`.

## Lifecycle policy and execution model
- Policy is DB-backed in `UserLifecyclePolicies` and exposed through Admin API/UI.
- Lifecycle sequence: **Deactivate -> Revoke tokens/authorizations -> Anonymize -> Optional Hard delete**.
- A background worker (`InactivityLifecycleWorker`) executes hourly in batches and processes inactivity and self-delete requests.
- Self-service deletion (`POST /auth/self/delete-request`) revokes sessions immediately and schedules downstream anonymization.

## Acceptance verification steps
1. Run migrations:
   - `dotnet ef database update --project src/AuthServer/AuthServer.csproj --startup-project src/AuthServer/AuthServer.csproj`
2. Verify policy endpoints in Swagger/Postman:
   - `GET /admin/api/security/user-lifecycle-policy`
   - `PUT /admin/api/security/user-lifecycle-policy`
   - `GET /admin/api/security/user-lifecycle/preview?days=90`
   - `POST /admin/api/security/users/{id}/deactivate`
   - `POST /admin/api/security/users/{id}/anonymize`
   - `POST /admin/api/security/users/{id}/hard-delete?confirm=true`
3. Verify token/session revocation:
   - Trigger deactivate/anonymize.
   - Confirm refresh token cannot be exchanged anymore and `/auth/session` returns unauthenticated for inactive/anonymized users.
4. Verify OIDC discovery/JWKS unchanged:
   - `GET /.well-known/openid-configuration`
   - `GET /.well-known/jwks`
5. Verify history retention:
   - Hard-delete a user with prior audit/error/incident rows.
   - Confirm rows remain and `ActorUserId` (or `UserId`) is `NULL` where applicable.
