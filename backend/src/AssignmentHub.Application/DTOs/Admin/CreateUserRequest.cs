namespace AssignmentHub.Application.DTOs.Admin;

/// <summary>
/// Payload for <c>POST /api/admin/users</c>.
/// </summary>
public sealed class CreateUserRequest
{
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    /// <summary>"Admin", "Teacher", or "Student".</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>Required for Student, forbidden for Admin/Teacher.</summary>
    public Guid? ClassRoomId { get; init; }
}
