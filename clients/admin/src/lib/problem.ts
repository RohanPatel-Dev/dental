/** RFC 9457 ProblemDetails, as the API's exception handler writes it. */
export interface ProblemDetails {
  readonly title?: string;
  readonly detail?: string;
  readonly status?: number;
  readonly correlationId?: string;
  readonly traceId?: string;
  /** Present on a validation failure: one entry per field, each with its messages. */
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

/** An API call that came back as a problem. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly problem: ProblemDetails,
  ) {
    super(problem.title ?? `The request failed with status ${status}.`);
    this.name = 'ApiError';
  }

  /** Per-field messages, empty when the failure was not a validation failure. */
  get fieldErrors(): Readonly<Record<string, readonly string[]>> {
    return this.problem.errors ?? {};
  }

  /** The identifier to quote to support. */
  get correlationId(): string | undefined {
    return this.problem.correlationId;
  }
}
