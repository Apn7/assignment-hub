namespace AssignmentHub.Application.DTOs.Auth;

/// <summary>Successful result of <c>POST /api/auth/login</c>.</summary>
public sealed class LoginResponse
{
    /// <summary>JWT to send as <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
    public string AccessToken { get; init; } = string.Empty;

    public DateTime ExpiresAtUtc { get; init; }

    public UserSummary User { get; init; } = new();
}
