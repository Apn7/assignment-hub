namespace AssignmentHub.Api.Contracts;

/// <summary>
/// Payload returned by <c>GET /api/health</c>.
/// </summary>
public sealed class HealthResponse
{
    public string Status { get; init; } = "Healthy";

    /// <summary>ASP.NET Core environment name (Development, Production, ...).</summary>
    public string Environment { get; init; } = string.Empty;

    public DateTimeOffset TimestampUtc { get; init; }
}
