namespace AssignmentHub.Application.DTOs.Auth;

/// <summary>
/// A signed access token and the moment it stops being valid.
/// </summary>
/// <param name="Token">Compact-serialised JWT, without the "Bearer " prefix.</param>
/// <param name="ExpiresAtUtc">Expiry in UTC, derived from configuration.</param>
public sealed record GeneratedToken(string Token, DateTime ExpiresAtUtc);
