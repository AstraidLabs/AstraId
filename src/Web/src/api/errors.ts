export type FieldErrors = Record<string, string[]>;

export type DiagnosticsDebug = {
  exceptionType?: string;
  stackTrace?: string;
  innerExceptionSummary?: string;
  path?: string;
  method?: string;
};

export type DiagnosticsInfo = {
  traceId?: string;
  errorId?: string;
  debug?: DiagnosticsDebug;
};

const statusMessages: Record<number, string> = {
  400: "Please check the entered data and try again.",
  401: "Your session has expired. Please sign in again.",
  403: "You don’t have permission to access this page.",
  404: "The requested resource was not found.",
  409: "This action conflicts with an existing record.",
  422: "Some fields are invalid. Please review and try again.",
  429: "Too many attempts. Please try again later.",
  500: "Something went wrong on our side. Please try again.",
  503: "The service is temporarily unavailable. Please try again later."
};

export class AppError extends Error {
  status: number;
  title?: string;
  detail?: string;
  traceId?: string;
  errorId?: string;
  fieldErrors?: FieldErrors;
  generalErrors?: string[];
  debug?: DiagnosticsDebug;

  constructor({
    status,
    title,
    detail,
    traceId,
    errorId,
    fieldErrors,
    generalErrors,
    debug
  }: {
    status: number;
    title?: string;
    detail?: string;
    traceId?: string;
    errorId?: string;
    fieldErrors?: FieldErrors;
    generalErrors?: string[];
    debug?: DiagnosticsDebug;
  }) {
    super(detail || title || statusMessages[status] || "Request failed.");
    this.name = "AppError";
    this.status = status;
    this.title = title;
    this.detail = detail;
    this.traceId = traceId;
    this.errorId = errorId;
    this.fieldErrors = fieldErrors;
    this.generalErrors = generalErrors;
    this.debug = debug;
  }
}

export function getStatusMessage(status: number) {
  return statusMessages[status] ?? "Request failed.";
}

export function normalizeFieldErrors(errors: unknown): FieldErrors | undefined {
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
      return [key, [] as string[]];
    })
    .filter(([, messages]) => messages.length > 0);
  if (entries.length === 0) {
    return undefined;
  }
  return Object.fromEntries(entries);
}

export async function parseErrorResponse(response: Response): Promise<AppError> {
  const contentType = response.headers.get("content-type") ?? "";
  let data: any = null;
  let text: string | null = null;

  if (contentType.includes("application/json") || contentType.includes("application/problem+json")) {
    try {
      data = await response.json();
    } catch {
      data = null;
    }
  }

  if (!data) {
    try {
      text = await response.text();
    } catch {
      text = null;
    }
  }

  const status = typeof data?.status === "number" ? data.status : response.status;
  const title = typeof data?.title === "string" ? data.title : response.statusText;
  const detail =
    typeof data?.detail === "string"
      ? data.detail
      : text || statusMessages[status];

  const fieldErrors = normalizeFieldErrors(data?.errors);
  const generalErrors = Array.isArray(data?.generalErrors)
    ? data.generalErrors.map(String)
    : undefined;

  return new AppError({
    status,
    title,
    detail,
    traceId: typeof data?.traceId === "string" ? data.traceId : undefined,
    errorId: typeof data?.errorId === "string" ? data.errorId : undefined,
    fieldErrors,
    generalErrors,
    debug: typeof data?.debug === "object" ? data.debug : undefined
  });
}
