# SignalR, MediatR, Hangfire, Redis integration

## Local development configuration (placeholders)

Use environment variables or user-secrets for sensitive values.

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379,abortConnect=false",
    "EnableSignalRBackplane": true
  },
  "Hangfire": {
    "StorageConnectionString": "<set-in-environment-or-user-secrets>"
  }
}
```

## Event contract and channel

- Channel: `app.events`
- Schema (`AstraId.Contracts.AppEvent`):
  - `Type`
  - `TenantId` (optional)
  - `UserId` (optional)
  - `EntityId`
  - `OccurredAt`
  - `Payload` (optional)

## Group naming in SignalR

- User group: `user:{UserId}`
- Tenant group: `tenant:{TenantId}`
- Event name: `app.{Type}` (example: `app.article.published`)

## Security notes

- Do not log access tokens, authorization headers, or raw connection strings.
- Keep Redis on internal/private network boundaries.
- Hangfire dashboard must be protected (local-only or admin policy).
- AuthServer and AppServer do not expose SignalR hubs publicly; only Api hosts hubs.

## Example curl commands (placeholders)

```bash
curl -X GET "https://localhost:7002/health"
curl -X GET "https://localhost:7003/health"
curl -X GET "https://localhost:7001/health"
curl -H "Authorization: Bearer <access_token_placeholder>" "https://localhost:7002/hubs/app/negotiate?negotiateVersion=1"
```
