import { AUTH_SERVER_BASE_URL } from "../../api/authServer";
import { pushToast } from "../components/toast";

export type FieldErrors = Record<string, string[]>;

export class ApiError extends Error {
  status: number;
  fieldErrors?: FieldErrors;
  title?: string;
  detail?: string;
  generalErrors?: string[];

  constructor(
    message: string,
    status: number,
    fieldErrors?: FieldErrors,
    title?: string,
    detail?: string,
    generalErrors?: string[]
  ) {
    super(message);
    this.status = status;
    this.fieldErrors = fieldErrors;
    this.title = title;
    this.detail = detail;
    this.generalErrors = generalErrors;
  }
}

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
      ...(requestOptions.headers ?? {}),
    },
  });

  if (!response.ok) {
    const error = await readError(response);
    if (!suppressToast) {
      pushToast({ message: error.message, tone: "error" });
    }
    throw error;
  }

  if (response.status === 204) {
    return null as T;
  }

  const text = await response.text();
  return (text ? JSON.parse(text) : null) as T;
}

async function readError(response: Response) {
  const contentType = response.headers.get("content-type") ?? "";
  if (contentType.includes("application/json")) {
    try {
      const data = await response.json();
      const errors = normalizeFieldErrors(data?.errors);
      const generalErrors = Array.isArray(data?.generalErrors)
        ? data.generalErrors.map(String)
        : undefined;
      if (typeof data?.title === "string") {
        return new ApiError(data.title, response.status, errors, data.title, data.detail, generalErrors);
      }
      if (typeof data?.detail === "string") {
        return new ApiError(data.detail, response.status, errors, data.title, data.detail, generalErrors);
      }
      if (typeof data?.message === "string") {
        return new ApiError(data.message, response.status, errors, data.title, data.detail, generalErrors);
      }
      if (errors) {
        const flat = Object.values(errors).flat().filter(Boolean).join(" ");
        if (flat) {
          return new ApiError(flat, response.status, errors, data.title, data.detail, generalErrors);
        }
      }
    } catch {
      return new ApiError(response.statusText, response.status);
    }
  }

  return new ApiError(response.statusText, response.status);
}

function normalizeFieldErrors(errors: unknown): FieldErrors | undefined {
  if (!errors || typeof errors !== "object") {
    return undefined;
  }
  const entries = Object.entries(errors as Record<string, unknown>)
    .map(([key, value]) => {
      if (Array.isArray(value)) {
        return [key, value.map(String)];
      }
      if (typeof value === "string") {
        return [key, [value]];
      }
      return [key, []];
    })
    .filter(([, value]) => value.length > 0);
  if (entries.length === 0) {
    return undefined;
  }
  return Object.fromEntries(entries);
}
