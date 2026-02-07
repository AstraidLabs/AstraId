import type { AuthSession } from "../api/authServer";
import type { AuthSessionStatus } from "./useAuthSession";

export const isAuthenticatedSession = (
  status: AuthSessionStatus,
  session: AuthSession | null
) => status === "authenticated" && Boolean(session?.isAuthenticated);
