# Development Runbook

## Prerequisites
- .NET SDK **10.x** (`TargetFramework` is `net10.0` for AuthServer/Api/AppServer).
- Node.js + npm for `src/Web` (Vite 7 toolchain; no fixed `engines` entry in `package.json`).
- PostgreSQL for AuthServer EF Core/OpenIddict storage.
- Redis for AuthServer, Api, and AppServer runtime paths.

## Local URLs and profiles
- AuthServer profile `AuthServer`: `https://localhost:7001`.
- Api profile `Api`: `https://localhost:7002`.
- AppServer profile `AppServer`: `https://localhost:7003` and `http://localhost:5003`.
- Web Vite dev server: `http://localhost:5173`.

## Start sequence (recommended)
1. Trust local certs:
   - `dotnet dev-certs https --check`
   - `dotnet dev-certs https --trust`
2. Start PostgreSQL and Redis.
3. Configure secrets using user-secrets or environment variables.
4. Start services:
   - `dotnet run --project src/AuthServer --launch-profile AuthServer`
   - `dotnet run --project src/Api --launch-profile Api`
   - `dotnet run --project src/AppServer --launch-profile AppServer`
   - `cd src/Web && npm install && npm run dev`

## Convenience scripts
- Linux/macOS: `scripts/dev.sh` starts AuthServer + Api + Web.
- PowerShell: `scripts/dev.ps1` starts AuthServer + Api + Web.
- Note: these scripts do **not** start AppServer, so `/app/*` paths require a separate AppServer process.

## Secrets management in development
Use placeholders in tracked config and inject real values via user-secrets/env.

Minimum secret/config alignment:
- AuthServer: `ConnectionStrings:DefaultConnection`, `Redis:ConnectionString`.
- Api: `InternalTokens:Jwks:InternalApiKey`, `Api:AuthServer:ApiKey`, `Redis:ConnectionString`.
- AppServer: `InternalTokens:JwksInternalApiKey`, `Redis:ConnectionString`.

Important contract:
- `InternalTokens:Jwks:InternalApiKey` (Api) and `InternalTokens:JwksInternalApiKey` (AppServer) must match.

## Common troubleshooting
### Certs / HTTPS
- Symptoms: browser/curl trust failures.
- Fix: rerun dev-certs trust; verify OS/browser trust store.

### CORS
- AuthServer and Api read explicit origin allow-lists.
- Ensure exact Web origin (`http://localhost:5173`) is present and no wildcard assumptions in hardened modes.

### DB migrations/startup
- AuthServer uses startup migration/bootstrap services.
- Verify PostgreSQL connectivity and `ConnectionStrings:DefaultConnection`.

### Policy-map authorization (Api)
- Api `EndpointAuthorizationMiddleware` denies unknown `/api/*` endpoints by default.
- Policy map is fetched from AuthServer endpoint `/admin/apis/{api}/policy-map` using `Api:AuthServer:*` settings and API key.
- Check `/ops/health` from Api for policy map refresh status when admin-authorized.

### Internal Api -> AppServer token path
- Api issues short-lived internal JWTs (`InternalTokenService`), AppServer validates issuer/audience/allowed service and rejects AuthServer user tokens.
- Verify issuer/audience/JWKS URL and internal API key alignment across Api/AppServer.
