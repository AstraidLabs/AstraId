# Configuration

Use `.env.example`, `.env.dev.example`, and `.env.prod.example` as templates. Copy to local `.env.dev`/`.env.prod` via `setup` commands.

## Critical environment variables
- `ConnectionStrings__DefaultConnection`
- `Redis__ConnectionString`
- `AuthServer__Issuer`
- `AuthServer__UiBaseUrl`
- `AUTHSERVER_ISSUER`
- `API_BASE_URL`
- `APPSERVER_BASE_URL`
- `WEB_BASE_URL`

Do not commit real secrets.
