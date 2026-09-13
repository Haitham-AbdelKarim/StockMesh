import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../models/paginated';

export class ApiError extends Error {
  readonly problem: ProblemDetails;
  readonly status: number;

  constructor(error: HttpErrorResponse) {
    const problem = error.error as ProblemDetails | null;
    const detail =
      problem?.detail ||
      problem?.title ||
      (error.error && typeof error.error === 'string' ? error.error : null) ||
      `Request failed with status ${error.status}.`;

    super(detail);
    this.name = 'ApiError';
    this.problem = problem ?? { detail };
    this.status = error.status;
  }

  fieldErrors(): Record<string, string[]> | undefined {
    return this.problem.errors;
  }
}

export function extractError(error: unknown): ApiError {
  if (error instanceof ApiError) {
    return error;
  }

  if (error instanceof HttpErrorResponse) {
    return new ApiError(error);
  }

  return new ApiError(
    new HttpErrorResponse({ status: 0, statusText: 'Unknown error', error: { detail: 'Unexpected error.' } }),
  );
}

export function firstDetail(error: unknown): string {
  const parsed = extractError(error);
  return parsed.message;
}