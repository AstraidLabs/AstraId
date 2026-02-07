import { createContext, useCallback, useContext, useEffect, useMemo, useState, type PropsWithChildren } from "react";
import { getSession, type AuthSession } from "../api/authServer";
import { AppError } from "../api/errors";

export type AuthSessionStatus = "loading" | "authenticated" | "anonymous";

type AuthSessionContextValue = {
  session: AuthSession | null;
  status: AuthSessionStatus;
  isLoading: boolean;
  error: string;
  refresh: () => Promise<void>;
};

const STATUS_STORAGE_KEY = "astraid.auth.status";

let cachedSession: AuthSession | null = null;
let cachedStatus: AuthSessionStatus = "loading";
let cachedError = "";
let refreshInFlight: Promise<void> | null = null;

const AuthSessionContext = createContext<AuthSessionContextValue | null>(null);

const deriveStatus = (session: AuthSession | null): AuthSessionStatus =>
  session?.isAuthenticated ? "authenticated" : "anonymous";

const getInitialStatus = (): AuthSessionStatus => {
  const persisted = sessionStorage.getItem(STATUS_STORAGE_KEY);
  return persisted === "authenticated" ? "authenticated" : "loading";
};

export const AuthSessionProvider = ({ children }: PropsWithChildren) => {
  const [session, setSession] = useState<AuthSession | null>(cachedSession);
  const [status, setStatus] = useState<AuthSessionStatus>(
    cachedStatus === "loading" ? getInitialStatus() : cachedStatus
  );
  const [error, setError] = useState(cachedError);

  const refresh = useCallback(async () => {
    if (refreshInFlight) {
      await refreshInFlight;
      return;
    }

    const run = async () => {
      setStatus((prev) => (prev === "authenticated" ? prev : "loading"));
      setError("");

      try {
        const data = await getSession();
        const nextSession = data;
        const nextStatus = deriveStatus(nextSession);
        setSession(nextSession);
        setStatus(nextStatus);
        cachedSession = nextSession;
        cachedStatus = nextStatus;
        cachedError = "";
        sessionStorage.setItem(STATUS_STORAGE_KEY, nextStatus);
      } catch (err) {
        const nextStatus: AuthSessionStatus = "anonymous";
        setSession(null);
        setStatus(nextStatus);

        if (err instanceof AppError) {
          setError(err.detail ?? err.message);
          cachedError = err.detail ?? err.message;
        } else if (err instanceof Error) {
          setError(err.message);
          cachedError = err.message;
        } else {
          setError("Unable to load session.");
          cachedError = "Unable to load session.";
        }

        cachedSession = null;
        cachedStatus = nextStatus;
        sessionStorage.setItem(STATUS_STORAGE_KEY, nextStatus);
      }
    };

    refreshInFlight = run().finally(() => {
      refreshInFlight = null;
    });

    await refreshInFlight;
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const value = useMemo<AuthSessionContextValue>(
    () => ({
      session,
      status,
      isLoading: status === "loading",
      error,
      refresh
    }),
    [error, refresh, session, status]
  );

  return <AuthSessionContext.Provider value={value}>{children}</AuthSessionContext.Provider>;
};

export const useAuthSession = () => {
  const context = useContext(AuthSessionContext);

  if (!context) {
    throw new Error("useAuthSession must be used within AuthSessionProvider.");
  }

  return context;
};
