# Game Local Dev Runbook

## Services

1. Start PostgreSQL + Redis.
2. Run AuthServer on `https://localhost:7001`.
3. Run Api on `https://localhost:7002`.
4. Run AppServer on `https://localhost:7003`.
5. Run GameClient (`src/GameClient`) on `http://localhost:5174`.

## Config

- AppServer connection strings:
  - `ConnectionStrings__GameDatabase`
- AppServer internal tokens:
  - `InternalTokens__Issuer`
  - `InternalTokens__Audience`
  - `InternalTokens__SigningKey`
- GameClient env:
  - `VITE_AUTH_AUTHORITY`
  - `VITE_AUTH_CLIENT_ID`
  - `VITE_AUTH_REDIRECT_URI`
  - `VITE_APP_SERVER_URL`

## Database migrations

- Migration is included under `src/AppServer/Migrations/202602180001_AddGameModule.cs`.
- AppServer calls `Database.Migrate()` on startup.

## End-to-end walkthrough

1. Open `http://localhost:5174/login` and authenticate via AuthServer.
2. Callback stores tokens and redirects to `/game`.
3. Game client requests `/api/game/state` and `/api/game/galaxy`.
4. Select system on Pixi map, issue `Survey/Colonize/Research` commands.
5. State panel reflects server-side tick/resource updates.
