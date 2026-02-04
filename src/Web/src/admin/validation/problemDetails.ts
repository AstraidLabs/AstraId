import { AppError, type DiagnosticsInfo, type FieldErrors } from "../api/http";

export type ParsedProblemDetails = {
  fieldErrors: FieldErrors;
  generalError: string | null;
  diagnostics?: DiagnosticsInfo;
};

export function parseProblemDetailsErrors(error: unknown): ParsedProblemDetails {
  if (error instanceof AppError) {
    const fieldErrors = error.fieldErrors ?? {};
    const generalError =
      error.generalErrors?.[0] ??
      error.detail ??
      error.message ??
      null;
    return {
      fieldErrors,
      generalError,
      diagnostics: {
        traceId: error.traceId,
        errorId: error.errorId,
        debug: error.debug
      }
    };
  }

  return { fieldErrors: {}, generalError: "Unexpected error." };
}
