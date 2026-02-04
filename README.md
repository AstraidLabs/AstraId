# AstraId ✨

AstraId je demonstrační řešení OAuth2/OIDC postavené na OpenIddict + ASP.NET Core Identity. Obsahuje autorizační server, chráněné API a React SPA (public + admin UI). Implementace vychází výhradně z aktuálního kódu a konfigurace v repozitáři.

---

## 📁 Struktura repozitáře

- `src/AuthServer` – OpenIddict autorizační server s Identity, admin API a hostováním admin UI (pokud je build k dispozici).
- `src/Api` – chráněné API se Swaggerem a OAuth2 konfigurací pro authorization code + PKCE.
- `src/Web` – React SPA (Vite) pro public UI i admin UI (build:admin).
- `src/Company.Auth.Contracts` – sdílené konstanty a definice clientů/scopů/permissions.
- `src/Company.Auth.Api` – sdílené rozšíření pro OpenIddict validation a permission policies v API.

---

## ✅ Funkcionality (stručně)

- **AuthServer**: OIDC endpoints (`/.well-known/openid-configuration`, `/connect/authorize`, `/connect/token`, `/connect/userinfo`, `/connect/logout`, `/connect/revocation`) a vlastní auth API `/auth/*` (login/registrace/aktivace/reset).
- **Admin UI + Admin API**: admin UI pod `/admin` (pokud existuje build) a admin API pod `/admin/api/*` (clients, roles, permissions, users, audit, OIDC scopes/resources).
- **Api**: chráněné endpointy `/api/*`, veřejný `/api/public`, admin `/api/admin/ping` a healthcheck `/health` s CORS pro Web SPA.
- **Seeding/migrace**: při startu AuthServer provede `Database.Migrate()` a synchronizuje permissions, scopes, clients a admin účet podle `AuthServerDefinitions` + `BootstrapAdmin`.
- **Company.Auth.***: sdílené konstanty (issuer, scopes, claim types) a helpery pro autorizaci v API.

---

## 🧰 Prerekvizity

- **.NET SDK**: repo neobsahuje `global.json`; projekty cílí na `net10.0` (potřebujete kompatibilní SDK).
- **Node.js + npm**: nutné pro build a běh SPA v `src/Web` (Vite).
- **PostgreSQL**: EF Core provider je Npgsql (connection string v `appsettings*.json`).
- (volitelné) **SMTP server** pro e-maily; v dev se defaultuje na `localhost:2525` (např. smtp4dev).

---

## 🚀 Instalace a spuštění (krok za krokem)

### 1) Restore

```bash
dotnet restore
```

### 2) Nastavení databáze (PostgreSQL)

Connection string je v:
- `src/AuthServer/appsettings.json` / `appsettings.Development.json` (`ConnectionStrings:DefaultConnection`).

Pokud používáte user-secrets, přepište stejný klíč `ConnectionStrings:DefaultConnection` v secrets.

### 3) Migrace / update DB

AuthServer migruje DB automaticky při startu. Ruční migrace:

```bash
dotnet ef database update --project src/AuthServer --startup-project src/AuthServer
```

### 4) Spuštění služeb

```bash
# AuthServer (https://localhost:7001)
dotnet run --project src/AuthServer --launch-profile AuthServer

# Api (https://localhost:7002)
dotnet run --project src/Api --launch-profile Api
```

Web SPA:

```bash
cd src/Web
npm install
npm run dev
```

> 🧩 **Admin UI build**: `dotnet build src/AuthServer` spustí `npm ci` + `npm run build:admin` a zkopíruje build do `src/AuthServer/wwwroot/admin-ui`.
> Admin UI pak běží na `https://localhost:7001/admin`.

---

## ⚙️ Konfigurace

### AuthServer

- **Issuer**: `AuthServer:Issuer` (musí být absolutní URL, v produkci HTTPS).
- **UI režim**: `AuthServer:UiMode` = `Separate` nebo `Hosted`. `UiBaseUrl` je pro separátní SPA (`http://localhost:5173`).
- **CORS**: `Cors:AllowedOrigins` (aktuálně `http://localhost:5173`).
- **Email** (SMTP): viz `Email:*` (Mode/From/Smtp) a validace v runtime; v dev se doplní defaulty pokud chybí.
- **Bootstrap admin**: `BootstrapAdmin` (Enabled, Email, Password, RoleName...).

### Api

- **Auth**: `Auth:Issuer`, `Auth:Audience`, `Auth:Scopes`.
- **Swagger OAuth**: `Swagger:OAuthClientId` (default `web-spa`).
- **CORS**: API povoluje `http://localhost:5173` (hard-coded v `Program.cs`).

### Web (Vite env)

V repo je pouze `.env.example`. **TODO: vytvořte `.env` s odpovídajícími hodnotami pro vaše prostředí** (nebo se spolehněte na defaulty v kódu).

Používané proměnné:

- `VITE_API_BASE_URL` (default `https://localhost:7002`).
- `VITE_AUTHSERVER_BASE_URL` (default `https://localhost:7001`).
- `VITE_AUTH_AUTHORITY`, `VITE_AUTH_CLIENT_ID`, `VITE_AUTH_REDIRECT_URI`, `VITE_AUTH_POST_LOGOUT_REDIRECT_URI`, `VITE_AUTH_SCOPE`.
- volitelné: `VITE_ADMIN_API_BASE_URL` (jinak použije `VITE_AUTHSERVER_BASE_URL`).
- Vite runtime parametry pro build (`VITE_BASE`, `VITE_OUT_DIR`, `VITE_ROUTER_BASE`) jsou použity ve `build` skriptech a `vite.config.ts`.

---

## 🔐 OIDC klient (napojení SPA)

### Jak vytvořit klienta

- **Seeding**: `AuthServerDefinitions` obsahuje klienta `web-spa` s redirect URI `http://localhost:5173/auth/callback`, PKCE a scope `openid profile email offline_access api`. Spouští se při startu AuthServeru (s migracemi).
- **Admin UI**: můžete spravovat klienty přes `/admin` a `/admin/api/clients` (vyžaduje policy `AdminOnly`).

### Nastavení redirect URI, scopes, PKCE

- **Redirect URI**: např. `http://localhost:5173/auth/callback` (seeding).
- **Scopes**: `openid profile email offline_access api` (seeding i Web `.env.example`).
- **PKCE**: public client používá authorization code + PKCE (v OpenIddict nastavuje requirements).

### Ověření flow

1. Získejte authorization code přes `https://localhost:7001/connect/authorize`.
2. Vyměňte code za token přes `https://localhost:7001/connect/token`.
3. Zavolejte chráněné API `GET https://localhost:7002/api/me` s access tokenem.

---

## 🌐 Základní URL přehled

- **AuthServer**: `https://localhost:7001` (launch profile).
- **Api**: `https://localhost:7002` (launch profile).
- **Web (public UI)**: `http://localhost:5173` (Vite dev server).
- **Admin UI**: `https://localhost:7001/admin` (statika ze `wwwroot/admin-ui`, pokud je build).

---

## 🔎 Endpointy a flow (výběr)

### AuthServer

- OIDC endpoints: `/.well-known/openid-configuration`, `/.well-known/jwks`, `/connect/authorize`, `/connect/token`, `/connect/userinfo`, `/connect/logout`, `/connect/revocation`.
- Auth API (`/auth/*`):
  - `POST /auth/login`
  - `POST /auth/register`
  - `POST /auth/forgot-password`
  - `POST /auth/reset-password`
  - `POST /auth/resend-activation`
  - `POST /auth/activate`
  - `POST /auth/logout`
  - `GET /auth/session`
- Admin API (`/admin/api/*`):
  - `/admin/api/clients`
  - `/admin/api/roles`
  - `/admin/api/permissions`
  - `/admin/api/users`
  - `/admin/api/me`
  - `/admin/api/audit`
  - `/admin/api/oidc/scopes`
  - `/admin/api/oidc/resources`
  - `/admin/api/api-resources`
- Admin ping: `GET /admin/ping` (policy `AdminOnly`).

### Api

- `GET /health` (healthcheck).
- `GET /api/public` (anonymous).
- `GET /api/me` (authenticated).
- `GET /api/admin/ping` (policy `RequireSystemAdmin`).
- `GET /api/integrations/authserver/ping` a `/api/integrations/cms/ping` (admin).

---

## 🧯 Troubleshooting

- **EF Core tools vs runtime**: projekty používají EF Core 10.0.x (Design/Identity) a Npgsql 10.0.0; použijte kompatibilní `dotnet-ef` verzi (10.0.x).
- **Issuer musí být absolutní URL** a v produkci HTTPS, jinak aplikace spadne při startu.
- **Email konfigurace**: v produkci musí být vyplněné `Email:FromEmail`, `Email:Smtp:Host`, `Email:Smtp:Port` (jinak start selže).
- **CORS/cookies**: AuthServer používá cookie `SameSite=None` a `Secure` (HTTPS); pokud UI běží separátně, povolte origin v `Cors:AllowedOrigins` a používejte HTTPS na AuthServeru.
