# AstraId OAuth2/OIDC Demo

Tento repozitář obsahuje kompletní demo řešení autentizace a autorizace:

- **AuthServer**: OpenIddict autorizační server + ASP.NET Identity (Razor shell pro admin UI).
- **Api**: chráněné ASP.NET Core API validující JWT access tokeny.
- **Web**: React SPA (Vite + Tailwind), která používá Authorization Code + PKCE.
- **Company.Auth.Contracts**: sdílené konstanty a definice klientů/scopů/permissions.
- **Company.Auth.Api**: sdílený balíček pro konfiguraci OpenIddict validation a policies v API.

## Prerekvizity

- .NET SDK 10
- Node.js 20+
- PostgreSQL (lokálně)

## Rychlý start

### 1) PostgreSQL
Vytvořte databázi `identity_demo` a uživatele `app`/`app`, nebo upravte connection string v `src/AuthServer/appsettings.Development.json`:

```
Host=localhost;Port=5432;Database=identity_demo;Username=app;Password=app
```

### 2) HTTPS certifikát

```
dotnet dev-certs https --trust
```

### 3) Migrace databáze (AuthServer)

AuthServer automaticky volá `Database.Migrate()` při startu. Pro ruční migraci:

```
dotnet ef database update --project src/AuthServer --startup-project src/AuthServer
```

### 4) Spuštění aplikací

V samostatných terminálech:

```
dotnet run --project src/AuthServer --launch-profile AuthServer

dotnet run --project src/Api --launch-profile Api

cd src/Web
npm install
npm run dev
```

Build admin UI (při prvním spuštění nebo po změnách):

```
dotnet build src/AuthServer
```

Admin UI (AuthServer):

1. Ujistěte se, že je admin UI build hotový (`dotnet build src/AuthServer`).
2. Spusťte AuthServer (`dotnet run --project src/AuthServer --launch-profile AuthServer`).
3. Otevřete `https://localhost:7001/admin`.

Nebo použijte připravené skripty:

```
./scripts/dev.sh
```

```
./scripts/dev.ps1
```

### 5) Ověření

1. Otevřete `http://localhost:5173`.
2. Klikněte na **Login** a zaregistrujte uživatele přes ASP.NET Identity UI.
3. Po návratu do SPA klikněte na **Call API**.
4. Zobrazí se odpověď z `/api/me`.
5. Pokud má uživatel permission `system.admin`, zobrazí se tlačítko **Admin Ping** a odpověď z `/api/admin/ping`.

## Návod na spuštění (detailní)

1. **Nainstalujte prerekvizity**
   - .NET SDK 10
   - Node.js 20+
   - PostgreSQL
2. **Připravte databázi**
   - vytvořte databázi `identity_demo` a uživatele `app`/`app`
   - případně upravte connection string v `src/AuthServer/appsettings.Development.json`
3. **Povolte HTTPS certifikát**
   - `dotnet dev-certs https --trust`
4. **Spusťte migrace**
   - migrace se spouští automaticky při startu AuthServeru
   - pro ruční migraci použijte: `dotnet ef database update --project src/AuthServer --startup-project src/AuthServer`
5. **Spusťte všechny služby**
   - AuthServer: `dotnet run --project src/AuthServer --launch-profile AuthServer`
   - API: `dotnet run --project src/Api --launch-profile Api`
   - Web:
     - `cd src/Web`
     - `npm install`
     - `npm run dev`
   - alternativně použijte skripty `./scripts/dev.sh` nebo `./scripts/dev.ps1`
6. **Ověřte funkčnost**
   - otevřete `http://localhost:5173`
   - přihlaste se přes **Login** a zkuste **Call API**

## Porty a URL

- AuthServer: `https://localhost:7001`
- Api: `https://localhost:7002`
- Web: `http://localhost:5173`
- Admin UI: `https://localhost:7001/admin`

## UI rozdělení (anti-chaos)

- `http://localhost:5173` = **public UI** pro běžné uživatele (login/registrace, obnovy hesla, aktivace, standalone home).
- `https://localhost:7001/admin` = **admin UI** hostované AuthServerem (vyžaduje permission `system.admin`).
- Přihlášení jako admin (dev): výchozí bootstrap účet je `admin@local.test` / `Password123!` (viz `BootstrapAdmin` v `appsettings.Development.json`).
- Ověření adminu: `GET https://localhost:7001/admin/ping` (nutné být přihlášen jako admin).
- Build admin UI: `dotnet build src/AuthServer` (spustí `npm ci` + `npm run build:admin` v `src/Web` a zkopíruje build do `src/AuthServer/wwwroot/admin-ui`).

## AuthServer – funkce

- **OAuth2/OIDC server** postavený na OpenIddict (authorization code + PKCE, refresh tokeny, userinfo, logout).【F:src/AuthServer/Program.cs†L97-L143】【F:src/AuthServer/Controllers/AuthorizationController.cs†L35-L177】
- **Vlastní auth API** pro registraci, login, aktivaci účtu a obnovu hesla (`/auth/*`).【F:src/AuthServer/Controllers/AuthController.cs†L41-L275】
- **Správa rolí a permission** (system/admin permission, mapování rolí, audit log při změnách).【F:src/AuthServer/Seeding/AuthServerDefinitions.cs†L7-L47】【F:src/AuthServer/Seeding/AuthBootstrapHostedService.cs†L256-L437】
- **Správa API resources a endpointů** (API key pro sync, policy map).【F:src/AuthServer/Controllers/ApiManagementController.cs†L10-L100】【F:src/AuthServer/Seeding/AuthBootstrapHostedService.cs†L318-L390】
- **Admin API** pro výpis klientů a permissions (chráněno policy `AdminOnly`).【F:src/AuthServer/Controllers/AdminController.cs†L10-L71】【F:src/AuthServer/Program.cs†L45-L61】
- **Email workflow** pro aktivace a reset hesla (SMTP).【F:src/AuthServer/Services/EmailTemplates.cs†L1-L41】【F:src/AuthServer/Options/EmailOptions.cs†L1-L29】
- **Rate limiting** pro citlivé auth akce (login/registrace/reset).【F:src/AuthServer/Controllers/AuthController.cs†L35-L189】【F:src/AuthServer/Services/AuthRateLimiter.cs†L1-L66】

## Jak AuthServer funguje

1. **Identity + DB**: ASP.NET Identity ukládá uživatele/role/permissions do PostgreSQL přes `ApplicationDbContext`.【F:src/AuthServer/Program.cs†L13-L33】
2. **OpenIddict server**: vystavuje standardní OIDC endpointy (`/connect/authorize`, `/connect/token`, `/connect/userinfo`, `/connect/logout`) a vydává JWT access tokeny (bez šifrování).【F:src/AuthServer/Program.cs†L97-L143】【F:src/AuthServer/Controllers/AuthorizationController.cs†L35-L170】
3. **Auth API**: Web UI komunikuje s `/auth/*` (login/registrace/aktivace/reset), validuje returnUrl a posílá e-maily s tokeny pro aktivaci/reset hesla.【F:src/AuthServer/Controllers/AuthController.cs†L46-L275】【F:src/AuthServer/Services/ReturnUrlValidator.cs†L1-L51】
4. **Seeding**: při startu se migruje DB a synchronizují se scope, clients, permissions a API resources podle `AuthServerDefinitions`.【F:src/AuthServer/Seeding/AuthBootstrapHostedService.cs†L33-L137】【F:src/AuthServer/Seeding/AuthServerDefinitions.cs†L7-L56】
5. **Admin role**: volitelně se bootstrapuje admin účet podle `BootstrapAdmin` konfigurace (v dev lze použít implicitní).【F:src/AuthServer/Seeding/AuthBootstrapHostedService.cs†L164-L312】【F:src/AuthServer/Options/BootstrapAdminOptions.cs†L1-L14】

## Režimy AuthServer (UI)

AuthServer umí běžet se dvěma režimy UI podle `AuthServer:UiMode`:

- **Separate** (default): UI běží zvlášť jako SPA (`UiBaseUrl`, typicky `http://localhost:5173`). AuthServer pouze generuje redirecty zpět do SPA.【F:src/AuthServer/Options/AuthServerUiOptions.cs†L5-L27】
- **Hosted**: AuthServer hostuje statické buildy SPA (složka `src/Web/dist` nebo cesta z `AuthServer:HostedUiPath`). V tomto režimu se statika obsluhuje přímo z AuthServeru a fallbackuje na `index.html`.【F:src/AuthServer/Options/AuthServerUiOptions.cs†L5-L27】【F:src/AuthServer/Program.cs†L158-L195】

## Správné nastavení AuthServer

1. **Issuer (OIDC)**  
   - Nastavte `AuthServer:Issuer` na veřejnou URL AuthServeru (např. `https://localhost:7001/`).【F:src/AuthServer/appsettings.json†L8-L12】
2. **UI režim**  
   - `AuthServer:UiMode` = `Separate` nebo `Hosted`.  
   - Pro `Separate` nastavte `AuthServer:UiBaseUrl`.  
   - Pro `Hosted` případně nastavte `AuthServer:HostedUiPath` (jinak se použije `src/Web/dist`).【F:src/AuthServer/Options/AuthServerUiOptions.cs†L5-L27】
3. **CORS**  
   - Pokud UI běží odděleně, přidejte jeho origin do `Cors:AllowedOrigins`.【F:src/AuthServer/Program.cs†L81-L90】【F:src/AuthServer/appsettings.json†L25-L29】
4. **Email (aktivace/reset)**  
   - Vyplňte `Email:FromEmail` a `Email:Smtp:*`.  
   - V produkci nesmí být prázdné (jinak start selže).【F:src/AuthServer/Program.cs†L150-L215】【F:src/AuthServer/Options/EmailOptions.cs†L1-L29】  
   - Detailní návod: `src/AuthServer/EmailSetup.md`.【F:src/AuthServer/EmailSetup.md†L1-L22】
5. **Bootstrap admin** (volitelně)  
   - Nastavte `BootstrapAdmin:Enabled`, `Email`, `Password` a případně `GeneratePasswordWhenMissing`.  
   - Pokud `OnlyInDevelopment=true`, admin se vytvoří jen v dev prostředí.【F:src/AuthServer/Options/BootstrapAdminOptions.cs†L1-L14】【F:src/AuthServer/Seeding/AuthBootstrapHostedService.cs†L164-L312】
6. **Klienti, scope a permissions**  
   - Upravujte v `AuthServerDefinitions` (scopes/clients/permissions). Změny se synchronizují při startu.【F:src/AuthServer/Seeding/AuthServerDefinitions.cs†L7-L56】【F:src/AuthServer/Seeding/AuthBootstrapHostedService.cs†L33-L137】

## Jak přidat nový projekt (SPA nebo API)

1. **Scope pro API**
   - přidejte nový scope do `AuthServerDefinitions.ApiResources` (např. `api.orders` nebo `api.orders.read`).
2. **Client pro SPA/web**
   - přidejte definici klienta do `AuthServerDefinitions.Clients`.
3. **Permissions a policies**
   - zapište nové permission do `AuthServerDefinitions.Permissions`.
   - v API použijte `PermissionPolicies` nebo vlastní policy (např. `RequirePermission(\"orders.read\")`).
4. **Konfigurace API**
   - nastavte `Auth:Issuer` a `Auth:Audience` v `appsettings.json`.
5. **Konfigurace Web**
   - upravte `scope`, `client_id`, `redirect_uri` a `post_logout_redirect_uri` v `src/Web/src/main.tsx`.

## Testování

- **Login**: `http://localhost:5173` → Login/Register přes Identity UI.
- **/api/me**: tlačítko **Call API** nebo přímý GET `https://localhost:7002/api/me`.
- **Admin ping**: `/api/admin/ping` (vyžaduje `system.admin` nebo roli Admin).

## Troubleshooting

- **CORS chyba**: ověřte, že AuthServer i Api povolují origin `http://localhost:5173`.
- **HTTPS cert**: spusťte `dotnet dev-certs https --trust` a restartujte aplikace.
- **redirect_uri mismatch**: zkontrolujte konfiguraci v `AuthServerDefinitions.Clients` a ve Web SPA.
- **issuer mismatch**: API musí validovat `https://localhost:7001/`.
- **chyby migrací / připojení DB**: zkontrolujte connection string v `appsettings.Development.json` a přístupová práva v PostgreSQL.
