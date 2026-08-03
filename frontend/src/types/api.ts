/**
 * Shapes returned by the ASP.NET Core API.
 *
 * The backend serialises with the default camelCase policy, so these mirror the
 * C# contracts in `backend/src/AssignmentHub.Api/Contracts` field for field.
 */

/** Payload of `GET /api/health`. */
export interface HealthResponse {
  status: string;
  environment: string;
  /** ISO-8601 timestamp. */
  timestampUtc: string;
}

/** The single error shape every failing endpoint returns. */
export interface ApiErrorResponse {
  status: number;
  title: string;
  detail?: string | null;
  /** Correlates the response with the server log entry. */
  traceId?: string | null;
  /** Field-level validation failures, keyed by property name. */
  errors?: Record<string, string[]> | null;
}
