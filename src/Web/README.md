# AstraId Web (React + Vite)

## Požadavky

- Node.js 20+
- AuthServer a Api běží lokálně

## Konfigurace

Zkopírujte `.env.example` do `.env` a případně upravte hodnoty:

```bash
cp .env.example .env
```

Proměnné:

- `VITE_AUTHSERVER_BASE_URL` (default `https://localhost:7001`)
- `VITE_AUTH_AUTHORITY` (default `https://localhost:7001`)
- `VITE_AUTH_CLIENT_ID` (default `web-spa`)
- `VITE_AUTH_REDIRECT_URI` (default `http://localhost:5173/auth/callback`)
- `VITE_AUTH_POST_LOGOUT_REDIRECT_URI` (default `http://localhost:5173/`)
- `VITE_AUTH_SCOPE` (default `openid profile email offline_access api`)
- `VITE_API_BASE_URL` (default `https://localhost:7002`)

## Spuštění

```bash
npm install
npm run dev
```

## Build

Public build (base "/"):

```bash
npm run build:public
```

Admin build (base "/admin"):

```bash
npm run build:admin
```

Default build (dist):

```bash
npm run build
```

## OIDC login flow

- `Login` přesměruje na AuthServer UI (`/connect/authorize`).
- Po přihlášení se SPA vrací na `/auth/callback` a uloží tokeny.
- `Logout` volá end-session endpoint (`/connect/logout`).
