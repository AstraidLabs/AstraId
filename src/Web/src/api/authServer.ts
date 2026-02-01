export const AUTH_SERVER_BASE_URL =
  import.meta.env.VITE_AUTHSERVER_BASE_URL ?? "https://localhost:7001";

const normalizePath = (path: string) =>
  path.startsWith("http")
    ? path
    : `${AUTH_SERVER_BASE_URL}${path.startsWith("/") ? "" : "/"}${path}`;

export type AuthResponse = {
  success: boolean;
  redirectTo?: string | null;
  error?: string | null;
};

export type AuthSession = {
  isAuthenticated: boolean;
  userId?: string | null;
  email?: string | null;
  userName?: string | null;
  roles: string[];
  permissions: string[];
};

export type AuthRequestOptions = RequestInit;

export const authFetch = async <T>(
  path: string,
  options: AuthRequestOptions = {}
): Promise<T> => {
  const response = await fetch(normalizePath(path), {
    credentials: "include",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      ...(options.headers ?? {})
    },
    ...options
  });

  if (!response.ok) {
    let details: unknown;
    try {
      details = await response.json();
    } catch {
      details = await response.text();
    }

    const message =
      typeof details === "object" && details && "error" in details
        ? String((details as { error?: string }).error ?? "Neznámá chyba.")
        : "Neznámá chyba.";

    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
};

export const login = (payload: {
  emailOrUsername: string;
  password: string;
  returnUrl?: string | null;
}) =>
  authFetch<AuthResponse>("/auth/login", {
    method: "POST",
    body: JSON.stringify(payload)
  });

export const register = (payload: {
  email: string;
  password: string;
  confirmPassword: string;
  returnUrl?: string | null;
}) =>
  authFetch<AuthResponse>("/auth/register", {
    method: "POST",
    body: JSON.stringify(payload)
  });

export const logout = () =>
  authFetch<void>("/auth/logout", {
    method: "POST"
  });

export const getSession = () => authFetch<AuthSession>("/auth/session");

export const resolveReturnUrl = (returnUrl: string | null) => {
  if (!returnUrl) {
    return null;
  }

  if (returnUrl.startsWith("/")) {
    return `${AUTH_SERVER_BASE_URL}${returnUrl}`;
  }

  return returnUrl;
};
