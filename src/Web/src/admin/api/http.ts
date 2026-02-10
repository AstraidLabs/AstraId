import { AUTH_SERVER_BASE_URL } from "../../api/authServer";
import { AppError, parseErrorResponse } from "../../api/errors";
import { pushToast } from "../components/toast";
import { getPreferredLanguageTag } from "../../i18n/language";

const ADMIN_API_BASE_URL =
  import.meta.env.VITE_ADMIN_API_BASE_URL ?? AUTH_SERVER_BASE_URL;

const normalizeUrl = (path: string) =>
  path.startsWith("http")
    ? path
    : `${ADMIN_API_BASE_URL}${path.startsWith("/") ? "" : "/"}${path}`;

export async function apiRequest<T>(
  url: string,
  options: (RequestInit & { suppressToast?: boolean }) = {}
) {
  const { suppressToast, ...requestOptions } = options;
  const response = await fetch(normalizeUrl(url), {
    credentials: "include",
    ...requestOptions,
    headers: {
      "Content-Type": "application/json",
      "Accept-Language": getPreferredLanguageTag(),
      ...(requestOptions.headers ?? {}),
    },
  });

  if (!response.ok) {
    const error = await parseErrorResponse(response);
    if (!suppressToast) {
      pushToast({
        message: error.message,
        tone: "error",
        diagnostics: {
          traceId: error.traceId,
          errorId: error.errorId,
          debug: error.debug
        }
      });
    }
    throw error;
  }

  if (response.status === 204) {
    return null as T;
  }

  const text = await response.text();
  return (text ? JSON.parse(text) : null) as T;
}

export type { FieldErrors, DiagnosticsInfo } from "../../api/errors";
export { AppError };
