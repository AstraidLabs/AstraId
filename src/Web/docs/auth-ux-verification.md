# Auth UX update: what changed / how to verify

## What changed
- Introduced explicit `GlobalLayout` naming for the standard site shell and kept `AuthLayout` dedicated to `/login` and `/register`.
- Kept global navbar free of any forgot-password entry.
- Updated Login/Register screens to use `lucide-react` icons in fields and actions.
- Preserved and surfaced a Home action inside each auth card.
- Added semantic heading support to the shared `Card` component so auth pages use `<h1>`.
- Added subtle auth card entrance animation with reduced-motion fallback.

## How to verify quickly
1. Start Web app:
   - `cd src/Web && npm run dev -- --host 0.0.0.0 --port 4173`
2. Open `/` while unauthenticated:
   - Navbar should show `Login` and `Register`.
   - Navbar should **not** show `Forgot password`.
3. Open `/login`:
   - No global top navbar should render.
   - `Forgot password?` should appear inside the Login form.
   - Inputs/actions should show icons and Home button should navigate to `/`.
4. Open `/register`:
   - No global top navbar should render.
   - Inputs/actions should show icons and Home button should navigate to `/`.
5. Build validation:
   - `cd src/Web && npm run build` should complete without import errors.
