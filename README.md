# AstraId OAuth2/OIDC Demo

Tento repozitář obsahuje kompletní demo řešení autentizace a autorizace:

- **AuthServer**: OpenIddict autorizační server + ASP.NET Identity (Razor Pages UI).
- **Api**: chráněné ASP.NET Core API validující JWT access tokeny.
- **Web**: React SPA (Vite + Tailwind), která používá Authorization Code + PKCE.

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

```
dotnet ef migrations add InitialCreate --project src/AuthServer --startup-project src/AuthServer

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

## Porty a URL

- AuthServer: `https://localhost:7001`
- Api: `https://localhost:7002`
- Web: `http://localhost:5173`

## Troubleshooting

- **CORS chyba**: ověřte, že AuthServer i Api povolují origin `http://localhost:5173`.
- **HTTPS cert**: spusťte `dotnet dev-certs https --trust` a restartujte aplikace.
- **redirect_uri mismatch**: zkontrolujte konfiguraci v AuthServer seedingu i ve Web SPA.
- **issuer mismatch**: API musí validovat `https://localhost:7001/`.
- **chyby migrací / připojení DB**: zkontrolujte connection string v `appsettings.Development.json` a přístupová práva v PostgreSQL.
