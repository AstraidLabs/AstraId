# Auth UX findings (Phase A)

## Routing and layout
- `App.tsx` already uses nested routes with separate layout wrappers:
  - `GlobalLayout` (formerly `AppLayout`) for public/app routes.
  - `AuthLayout` for `/login` and `/register`.
  - `AdminLayout` remains dedicated under the admin route prefix.
- `GlobalLayout` contains semantic `<header>`, `<main>`, and `<footer>` structure.
- `AuthLayout` intentionally omits global navigation and centers auth content.

## Forgot password visibility
- Global navbar (`TopNav.tsx`) does not render any `Forgot password` link.
- The `Forgot password?` link is rendered only in `Login.tsx`, next to the password label.

## Auth state and navbar decision logic
- Session state is driven by `AuthSessionProvider` (`useAuthSession.tsx`) calling `/auth/session` through `getSession()`.
- `TopNav.tsx` uses `status === "anonymous"` to show Login/Register links.
- For authenticated users, `TopNav.tsx` shows account controls (Account dropdown + language selector) and `Admin` when authorized.

## Security model check
- Cookie-based auth session is preserved via `credentials: "include"` in `authFetch` (`authServer.ts`).
- No token persistence in `localStorage`; only minimal session status hint is stored in `sessionStorage`.

## Icon usage
- `lucide-react` is already installed in `src/Web/package.json` and available for use.

## UI baseline before implementation
- Login/Register already had card-based UI, inline icons, and home button.
- Auth background already had starfield effect with reduced motion support.
- Improvements focused on modernizing icon set and consistency, strengthening semantic heading usage, and adding subtle card entrance motion.
