import { useCallback, useEffect, useState } from "react";
import { getSession, type AuthSession } from "../api/authServer";
import { AppError } from "../api/errors";

export const useAuthSession = () => {
  const [session, setSession] = useState<AuthSession | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>("");

  const refresh = useCallback(async () => {
    setIsLoading(true);
    setError("");

    try {
      const data = await getSession();
      setSession(data);
    } catch (err) {
      setSession(null);
      if (err instanceof AppError) {
        setError(err.detail ?? err.message);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Unable to load session.");
      }
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  return {
    session,
    isLoading,
    error,
    refresh
  };
};
