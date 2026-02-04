# AstraId ✨

AstraId je demonstrační řešení OAuth2/OIDC postavené na OpenIddict + ASP.NET Core Identity. Obsahuje autorizační server, chráněné API a React SPA (public + admin UI). Implementace vychází výhradně z aktuálního kódu a konfigurace v repozitáři.

---

## Proč AstraId (k čemu je)

AstraId slouží jako **centrální Identity + OIDC provider** pro více aplikací (SPA, API, serverové aplikace) v rámci jednoho issueru. V praxi přináší jednotné přihlášení (SSO v rámci stejného issueru/originu), jednotné tokeny/claims a centralizovanou správu klientů, scopes/resources, uživatelů, rolí a permissions přes AuthServer + admin UI/API. Aktuálně podporované OIDC endpointy jsou `/.well-known/openid-configuration`, `/connect/authorize`, `/connect/token`, `/connect/userinfo`, `/connect/logout`, `/connect/revocation` a vlastní auth API `/auth/*` (login/registrace/aktivace/reset/session).

### Výhody

**Technické**
- OIDC/OAuth2 standard s **Authorization Code + PKCE** (server povoluje pouze authorization_code + refresh_token).
- Centralizovaný issuer (`AuthServer:Issuer`) a jednotná validace tokenů v API přes `AddCompanyAuth` (OpenIddict Validation).
- Permission‑based autorizace přes claim `permission` a policy (např. `system.admin`) napříč službami.
- Admin audit log změn (admin API `/admin/api/audit`).
- Automatické migrace + seedování scopes/clients/permissions/admin účtu při startu AuthServeru (AuthBootstrapHostedService + AuthServerDefinitions).

**Praktické / uživatelské**
- Jeden účet pro více aplikací a jednotné přihlášení přes `/connect/authorize` + společný issuer.
- Konzistentní login/register/recovery UX přes `/auth/*` + UI režim `Separate/Hosted` (UiMode + UiBaseUrl).
- Centralizovaná správa přístupů (role/permissions) bez zásahů do každé aplikace – permission claimy se vystavují do tokenu i session odpovědi.

**Byznysové**
- Rychlejší onboarding nové aplikace: přidáte klienta, scopes a redirect URI v admin UI/API a použijete jednotný issuer/scopes/audience v klientovi + API.
- Jednotné řízení přístupů a compliance (centralizované audit logy admin změn).
- Nižší náklady na údržbu auth logiky v každé aplikaci díky sdíleným helperům a jednotnému standardu.

### Kdy AstraId použít
- Máte více aplikací (SPA, API, admin portály), které musí sdílet identitu a jednotné tokeny/claims.
- Přístupy/role/permissions se často mění a potřebujete je řídit centrálně přes admin UI/API.
- Chcete konzistentní OIDC flow (Authorization Code + PKCE) napříč klienty.

### Limity a co to není
- **Grant types**: server povoluje pouze `authorization_code` a `refresh_token` (žádný `client_credentials`, `password`, `implicit`).
- **Externí identity/federace**: aktuálně v repu nevidím integraci s Google/Microsoft nebo jinými IdP (žádné externí sign-in provider konfigurace v AuthServer).
- **Multi‑tenant model**: existuje claim `tenant`, ale v kódu nevidím skutečný tenant model ani tenant‑aware autorizaci (aktuálně je to limit).
- **Key management**: v dev se používají development certifikáty; v produkci musíte dodat signing/encryption certy – UI pro rotaci signing keys aktuálně nevidím.
- **SSO jen v rámci stejného issueru/originu** (cookie‑based session); cross‑domain SSO bez sdíleného issueru zde není řešené. Cookie je `SameSite=None; Secure` a vyžaduje HTTPS + správné CORS/credentials nastavení.
- **SPOF riziko**: AuthServer je centrální bod, bez HA/monitoringu je výpadek kritický (potřeba řešit dostupnost v nasazení).

### Jak musí být aplikace připravena (checklist)

**1) SPA klient (React/Vite)**
- ✅ Umí Authorization Code + PKCE (react-oidc-context / oidc-client-ts).
- ✅ Nastaví `redirect_uri` a `post_logout_redirect_uri` (např. `http://localhost:5173/auth/callback`).
- ✅ Pracuje se scopes `openid profile email offline_access api` (nebo dle adminu).
- ✅ Pro cookie‑based session volá `/auth/session` s `credentials: "include"` (SSO v rámci issueru).
- ✅ Token ukládá bezpečně (aktuálně Web používá `sessionStorage`).

**2) Backend API**
- ✅ Validuje JWT proti issueru pomocí OpenIddict Validation (`AddCompanyAuth`).
- ✅ Nastaví audience (v repo default `api`).
- ✅ Vynucuje policies s permission claimem `permission` (např. `system.admin`).
- ✅ Swagger OAuth2 nastavený na Authorization Code + PKCE (pokud používáte Swagger UI).

**3) Server aplikace (confidential client)**
- ✅ Pokud chcete confidential klienta, musí mít `client_secret` (spravuje admin UI/API).
- ✅ Secret drží bezpečně (user-secrets/KeyVault/env) – v repo není automatizované uložení secretů.
- ⚠️ Pozn.: server aktuálně nepovoluje `client_credentials`, takže typické M2M scénáře je potřeba řešit jinak nebo rozšířit konfiguraci serveru.

### Typický integrační postup
1) **V admin UI/API vytvořit API resource** (`/admin/api/api-resources`).
2) **V admin UI/API vytvořit scopes** a přiřadit je resource (`/admin/api/oidc/scopes`, `/admin/api/oidc/resources`).
3) **V admin UI/API vytvořit clienta** (public/confidential), nastavit grant types, redirect URI a scopes (`/admin/api/clients`).
4) **V klientovi nastavit** `authority/issuer`, `client_id`, `redirect_uri`, `scopes` (SPA: Authorization Code + PKCE).
5) **V API nastavit** issuer/audience a permission policies (`AddCompanyAuth`, `RequirePermission`).
6) **Ověřit flow**: `/connect/authorize` → `/connect/token` → volání API s bearer tokenem → `/connect/userinfo`.

## 📁 Struktura repozitáře

- `src/AuthServer` – OpenIddict autorizační server s Identity, admin API a hostováním admin UI (pokud je build k dispozici).
- `src/Api` – chráněné API se Swaggerem a OAuth2 konfigurací pro authorization code + PKCE.
- `src/Web` – React SPA (Vite) pro public UI i admin UI (build:admin).
- `src/Company.Auth.Contracts` – sdílené konstanty a definice clientů/scopů/permissions.
- `src/Company.Auth.Api` – sdílené rozšíření pro OpenIddict validation a permission policies v API.

---

## ✅ Funkcionality (stručně)

- **AuthServer**: OIDC endpoints (`/.well-known/openid-configuration`, `/connect/authorize`, `/connect/token`, `/connect/userinfo`, `/connect/logout`, `/connect/revocation`) a vlastní auth API `/auth/*` (login/registrace/aktivace/reset/MFA).
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

## 🔐 MFA (TOTP / 2FA)

AstraId podporuje **TOTP MFA** přes ASP.NET Identity. MFA je řešené jako API v AuthServeru a UI v React (public web). Klíčové vlastnosti:

- Uživatel si může MFA zapnout/vypnout.
- Přihlášení vyžaduje MFA kód nebo recovery code, pokud má MFA aktivní.
- MFA flow funguje i při `/connect/authorize` (returnUrl pokračuje po ověření).
- Recovery codes se zobrazují pouze jednou a je nutné je bezpečně uložit.

### API endpointy

**Přihlášení**
- `POST /auth/login` → při MFA vrací `{ requiresTwoFactor: true, mfaToken, redirectTo }`.
- `POST /auth/login/mfa` → dokončení MFA challenge.

**Správa MFA (vyžaduje auth cookie)**
- `GET /auth/mfa/status`
- `POST /auth/mfa/setup/start` → shared key + QR (SVG)
- `POST /auth/mfa/setup/confirm` → aktivace + recovery codes
- `POST /auth/mfa/recovery-codes/regenerate`
- `POST /auth/mfa/disable`

### Zapnutí MFA (rychlý postup)
1. Přihlaste se do public UI (`/login`).
2. Otevřete **Account → Security** (`/account/security`).
3. Spusťte nastavení MFA → naskenujte QR v authenticator aplikaci.
4. Potvrďte kód, uložte recovery codes.

### Ověření flow (manuálně)
1. Registrace → login bez MFA.
2. Zapnutí MFA (setup + confirm).
3. Logout.
4. Login → vyžádán MFA challenge.
5. Login přes recovery code.
6. Regenerace recovery codes.
7. Disable MFA.

### Bezpečnostní poznámky
- MFA challenge token je krátkodobý (5 min) a jednorázový.
- MFA kódy/recovery codes se nelogují.
- Rate limiting chrání `/auth/login` a `/auth/login/mfa`.

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
