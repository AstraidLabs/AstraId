import { AUTH_SERVER_BASE_URL } from "../../api/authServer";
import { pushToast } from "../components/toast";

const ADMIN_API_BASE_URL =
  import.meta.env.VITE_ADMIN_API_BASE_URL ?? AUTH_SERVER_BASE_URL;

const normalizeUrl = (path: string) =>
  path.startsWith("http")
    ? path
    : `${ADMIN_API_BASE_URL}${path.startsWith("/") ? "" : "/"}${path}`;

export async function apiRequest<T>(url: string, options: RequestInit = {}) {
  const response = await fetch(normalizeUrl(url), {
    credentials: "include",
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(options.headers ?? {}),
    },
  });

  if (!response.ok) {
    const message = await readErrorMessage(response);
    pushToast({ message, tone: "error" });
    throw new Error(message);
  }

  if (response.status === 204) {
    return null as T;
  }

  const text = await response.text();
  return (text ? JSON.parse(text) : null) as T;
}

async function readErrorMessage(response: Response) {
  const contentType = response.headers.get("content-type") ?? "";
  if (contentType.includes("application/json")) {
    try {
      const data = await response.json();
      if (typeof data?.title === "string") {
        return data.title;
      }
      if (typeof data?.detail === "string") {
        return data.detail;
      }
      if (typeof data?.message === "string") {
        return data.message;
      }
      if (data?.errors) {
        const errors = Object.values(data.errors).flat().filter(Boolean).join(" ");
        if (errors) {
          return errors;
        }
      }
    } catch {
      return response.statusText;
    }
  }

  return response.statusText;
}
