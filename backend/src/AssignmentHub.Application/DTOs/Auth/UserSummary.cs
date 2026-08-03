using AssignmentHub.Domain.Entities;

namespace AssignmentHub.Application.DTOs.Auth;

/// <summary>
/// The safe public projection of a user. Note the absence of
/// <see cref="User.PasswordHash"/> — this type exists so a hash can never be
/// serialised to a client by accident.
/// </summary>
public sealed class UserSummary
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    /// <summary>Role name, e.g. "Admin".</summary>
    public string Role { get; init; } = string.Empty;

    public static UserSummary FromUser(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role.ToString()
    };
}
