export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7002";

const normalizePath = (path: string) =>
  path.startsWith("http") ? path : `${API_BASE_URL}${path.startsWith("/") ? "" : "/"}${path}`;

import { AppError, parseErrorResponse } from "./errors";
export { AppError } from "./errors";

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
    throw await parseErrorResponse(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
};
