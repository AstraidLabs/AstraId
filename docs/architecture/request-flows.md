# Request Flows

## 1) OIDC authorization code flow (implemented)
1. Web initializes OIDC settings (`authority`, `client_id`, `response_type: code`).
2. Browser is redirected to AuthServer `/connect/authorize`.
3. AuthServer `AuthorizationController.Authorize` validates request/user/consent context.
4. AuthServer issues authorization code and later tokens via `/connect/token`.
5. Web stores session/user info through oidc-client-ts and uses bearer access token for API calls.

## 2) Api policy-map enforcement flow (implemented)
1. Request hits Api `/api/*`.
2. `EndpointAuthorizationMiddleware` checks authentication and required scope.
3. Middleware queries `PolicyMapClient.FindRequiredPermissions(method,path)`.
4. If endpoint not mapped, deny by default (403).
5. If mapped with required permissions, caller claims must contain all permissions.
6. Authorized request proceeds to endpoint handler.

## 3) Api -> AppServer internal-token call (implemented)
1. Api authenticates user token (AuthServer-issued).
2. Api `InternalTokenService.CreateToken` mints short-lived JWT containing service claims (`svc=api`) and optional tenant claim.
3. Api sends request to AppServer with internal bearer token.
4. AppServer JWT auth validates signature/issuer/audience/allowed service.
5. AppServer explicitly rejects AuthServer user tokens and invalid service claims.

## 4) Redis event -> SignalR fanout (implemented)
1. Producer publishes `AstraId.Contracts.AppEvent` to Redis `EventChannels.AppEvents`.
2. Api `RedisEventSubscriber` listens on the channel.
3. Subscriber deserializes event and emits SignalR messages via `AppHub`:
   - user-scoped group `user:{userId}`
   - tenant-scoped group `tenant:{tenantId}`
4. Connected authorized clients receive `app.{eventType}` events.
