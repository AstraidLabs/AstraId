# AstraId OAuth2/OIDC Demo

Tento repozitář obsahuje kompletní demo řešení autentizace a autorizace:

- **AuthServer**: OpenIddict autorizační server + ASP.NET Identity (Razor Pages UI).
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

## Porty a URL

- AuthServer: `https://localhost:7001`
- Api: `https://localhost:7002`
- Web: `http://localhost:5173`

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
