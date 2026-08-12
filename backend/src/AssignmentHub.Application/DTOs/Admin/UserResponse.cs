namespace AssignmentHub.Application.DTOs.Admin;

/// <summary>
/// Safe public projection of a user for admin listings. Note the deliberate
/// absence of <c>PasswordHash</c> — this type exists so a hash can never be
/// serialised to a client by accident.
/// </summary>
public sealed class UserResponse
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    /// <summary>Role name, e.g. "Admin".</summary>
    public string Role { get; init; } = string.Empty;

    public Guid? ClassRoomId { get; init; }

    /// <summary>Resolved class name, populated for students.</summary>
    public string? ClassRoomName { get; init; }
}
