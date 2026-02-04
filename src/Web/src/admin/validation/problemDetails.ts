import { ApiError, type FieldErrors } from "../api/http";

export type ParsedProblemDetails = {
  fieldErrors: FieldErrors;
  generalError: string | null;
};

export function parseProblemDetailsErrors(error: unknown): ParsedProblemDetails {
  if (error instanceof ApiError) {
    const fieldErrors = error.fieldErrors ?? {};
    const generalError =
      error.generalErrors?.[0] ??
      error.detail ??
      error.message ??
      null;
    return { fieldErrors, generalError };
  }

  return { fieldErrors: {}, generalError: "Unexpected error." };
}
