namespace AssignmentHub.Application.DTOs.Auth;

/// <summary>Credentials posted to <c>POST /api/auth/login</c>.</summary>
public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
