import { useCallback, useEffect, useState } from "react";
import { getSession, type AuthSession } from "../api/authServer";

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
      setError(
        err instanceof Error ? err.message : "Nepodařilo se načíst session."
      );
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
