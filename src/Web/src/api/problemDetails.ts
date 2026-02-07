export type ParsedValidationProblemDetails = {
  kind: "validation";
  status: number;
  title?: string;
  detail?: string;
  errorId?: string;
  traceId?: string;
  fieldErrors: Record<string, string[]>;
};

export type ParsedProblemDetails = {
  kind: "problem";
  status: number;
  title?: string;
  detail?: string;
  errorId?: string;
  traceId?: string;
};

export type ParsedProblemResult = ParsedValidationProblemDetails | ParsedProblemDetails;

const normalizeFieldErrors = (errors: unknown): Record<string, string[]> => {
  if (!errors || typeof errors !== "object") {
    return {};
  }

  return Object.fromEntries(
    Object.entries(errors as Record<string, unknown>)
      .map(([key, value]) => {
        if (Array.isArray(value)) {
          return [key, value.map(String)];
        }
        if (typeof value === "string") {
          return [key, [value]];
        }
        return [key, [] as string[]];
      })
      .filter(([, messages]) => messages.length > 0)
  );
};

const toText = async (response: Response) => {
  try {
    return await response.text();
  } catch {
    return "";
  }
};

export const parseProblemDetails = async (response: Response): Promise<ParsedProblemResult> => {
  const contentType = response.headers.get("content-type") ?? "";

  let payload: Record<string, unknown> | null = null;
  if (contentType.includes("application/json") || contentType.includes("application/problem+json")) {
    try {
      payload = (await response.json()) as Record<string, unknown>;
    } catch {
      payload = null;
    }
  }

  if (!payload) {
    const detail = await toText(response);
    return {
      kind: "problem",
      status: response.status,
      title: response.statusText || "Request failed",
      detail: detail || "Request failed"
    };
  }

  const status = typeof payload.status === "number" ? payload.status : response.status;
  const title = typeof payload.title === "string" ? payload.title : response.statusText;
  const detail = typeof payload.detail === "string" ? payload.detail : undefined;
  const errorId = typeof payload.errorId === "string" ? payload.errorId : undefined;
  const traceId = typeof payload.traceId === "string" ? payload.traceId : undefined;

  const fieldErrors = normalizeFieldErrors(payload.errors);

  if (Object.keys(fieldErrors).length > 0) {
    return {
      kind: "validation",
      status,
      title,
      detail,
      errorId,
      traceId,
      fieldErrors
    };
  }

  return {
    kind: "problem",
    status,
    title,
    detail,
    errorId,
    traceId
  };
};
