export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7002";

const normalizePath = (path: string) =>
  path.startsWith("http") ? path : `${API_BASE_URL}${path.startsWith("/") ? "" : "/"}${path}`;

export class ApiError extends Error {
  status: number;
  details?: unknown;

  constructor(message: string, status: number, details?: unknown) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.details = details;
  }
}

export type ApiFetchOptions = RequestInit & {
  token?: string;
};

export const apiFetch = async <T>(
  path: string,
  options: ApiFetchOptions = {}
): Promise<T> => {
  const { token, headers, ...rest } = options;
  const response = await fetch(normalizePath(path), {
    ...rest,
    headers: {
      Accept: "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(headers ?? {})
    }
  });

  if (!response.ok) {
    let details: unknown;
    try {
      details = await response.json();
    } catch {
      details = await response.text();
    }

    const message =
      response.status === 401
        ? "Nejste přihlášeni."
        : response.status === 403
          ? "Nemáte oprávnění na tuto akci."
          : `API chyba (${response.status}).`;

    throw new ApiError(message, response.status, details);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
};
