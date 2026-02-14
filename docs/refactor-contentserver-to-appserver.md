# Refactor Report: ContentServer -> AppServer

## Summary
This refactor performed a global rename of **ContentServer** to **AppServer** across the repository with minimal behavioral changes.

### Renamed items
- **Project/folder/file names**
  - `src/ContentServer` -> `src/AppServer`
  - `src/ContentServer/ContentServer.csproj` -> `src/AppServer/AppServer.csproj`
  - `src/Api/Integrations/ContentServerClient.cs` -> `src/Api/Integrations/AppServerClient.cs`
  - `docs/architecture/auth-api-content-analysis.md` -> `docs/architecture/auth-api-app-analysis.md`
- **Solution reference**
  - Updated `AstraId.sln` project entry from `ContentServer` to `AppServer` and path to new `.csproj`.
- **Namespaces/types/usings**
  - `ContentServer.Security` namespace -> `AppServer.Security`
  - `ContentServerClient` type -> `AppServerClient`
- **Configuration and contracts**
  - Internal token audience: `astraid-content` -> `astraid-app`
  - Added API service config section `Services:AppServer` for AppServer downstream integration.
- **Routes/endpoints**
  - App-facing endpoint segment `/content` -> `/app`
  - Downstream AppServer client paths updated to `/app/items`

## Categorized changed files

### Solution/project structure
- `AstraId.sln`
- `src/AppServer/AppServer.csproj`
- `src/AppServer/**` (moved from `src/ContentServer/**`)

### API integration and routing
- `src/Api/Program.cs`
- `src/Api/Integrations/AppServerClient.cs`
- `src/Api/Integrations/InternalTokenHandler.cs`
- `src/Api/Integrations/ServiceNames.cs`

### Configuration
- `src/Api/appsettings.json`
- `src/Api/appsettings.Development.json`
- `src/Api/Options/InternalTokenOptions.cs`
- `src/AppServer/appsettings.json`
- `src/AppServer/appsettings.Development.json`
- `src/AppServer/Properties/launchSettings.json`
- `src/AppServer/Security/InternalTokenOptions.cs`

### AppServer code (namespace/route updates)
- `src/AppServer/Program.cs`
- `src/AppServer/Security/CurrentUser.cs`
- `src/AppServer/Security/ScopeParser.cs`

### Docs
- `README.md`
- `src/Api/README.md`
- `docs/architecture/auth-api-app-analysis.md`

## Breaking changes

> **Important:** This is a global rename and includes public API route changes.

### Route changes
- `GET /content/items` -> `GET /app/items`
- `POST /content/items` -> `POST /app/items`

### Internal token audience
- `aud=astraid-content` -> `aud=astraid-app`

### Project path/name changes
- Tooling/scripts referring to `src/ContentServer` or `ContentServer.csproj` must be updated to `src/AppServer` and `AppServer.csproj`.

## Manual follow-up steps required
Update any external or deployment assets not present (or not authoritative) in this repo:
- CI/CD pipeline project paths and build targets (`ContentServer` -> `AppServer`)
- Docker Compose service names and references (if defined externally)
- Kubernetes manifests (Deployment, Service, Ingress, ConfigMap, Secret key names)
- Reverse proxy routes (Nginx/Envoy/YARP) for `/content/*` -> `/app/*`
- Environment variables/config maps for:
  - Internal audience (`astraid-app`)
  - Service section/keys (`Services:AppServer:*`)
- Any generated SDKs, Postman collections, or API gateway route maps using `/content`

## Post-refactor verification
Run these commands in a .NET-enabled environment:

```bash
# Build entire solution
 dotnet build AstraId.sln

# Optionally run each service
 dotnet run --project src/AuthServer
 dotnet run --project src/Api
 dotnet run --project src/AppServer --launch-profile AppServer
```

Smoke checks:
- Confirm API route responses on `/app/items`.
- Confirm internal token audience validation expects `astraid-app`.
- Confirm AppServer receives proxied requests from API and rejects non-internal tokens.

## Safety checks completed
- Repo-wide search for `ContentServer` completed: no active occurrences remain.
- Repo-wide search for `/content` endpoint paths completed: endpoint paths were renamed to `/app`.
