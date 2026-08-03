namespace AssignmentHub.Api.Contracts;

/// <summary>
/// The single error shape every failing endpoint returns, so the frontend only
/// ever has to parse one thing. Modelled on RFC 7807 without pulling in the
/// full ProblemDetails contract.
/// </summary>
public sealed class ApiErrorResponse
{
    /// <summary>HTTP status code, repeated in the body for convenience.</summary>
    public int Status { get; init; }

    /// <summary>Short, human-readable summary safe to show to an end user.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Longer explanation. Stack traces are only included outside Production.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>Correlates the response with the server log entry.</summary>
    public string? TraceId { get; init; }

    /// <summary>Field-level validation failures, keyed by property name.</summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
