import { AppError } from "../api/errors";
import type { ParsedProblemResult } from "../api/problemDetails";

export const mapErrorToProblem = (error: unknown, fallback: string): ParsedProblemResult => {

  if (
    error &&
    typeof error === "object" &&
    "kind" in error &&
    ((error as ParsedProblemResult).kind === "problem" || (error as ParsedProblemResult).kind === "validation")
  ) {
    return error as ParsedProblemResult;
  }

  if (error instanceof AppError) {
    if (error.fieldErrors && Object.keys(error.fieldErrors).length > 0) {
      return {
        kind: "validation",
        status: error.status,
        title: error.title,
        detail: error.detail ?? error.message,
        traceId: error.traceId,
        errorId: error.errorId,
        fieldErrors: error.fieldErrors
      };
    }

    return {
      kind: "problem",
      status: error.status,
      title: error.title,
      detail: error.detail ?? error.message,
      traceId: error.traceId,
      errorId: error.errorId
    };
  }

  if (error instanceof Error) {
    return {
      kind: "problem",
      status: 500,
      title: "Unexpected error",
      detail: error.message,
    };
  }

  return {
    kind: "problem",
    status: 500,
    title: "Unexpected error",
    detail: fallback
  };
};
