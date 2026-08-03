using AssignmentHub.Domain.Enums;

namespace AssignmentHub.Domain.Entities;

/// <summary>
/// An account in the system. A single table holds all three roles because they
/// share every field except class membership.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    /// <summary>Login identifier. Unique across all users.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Output of <c>PasswordHasher&lt;User&gt;</c>. Never stores a plaintext password.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    /// <summary>
    /// The class this user belongs to. Set for students only; null for Admin and
    /// Teacher, whose relationship to a class runs through
    /// <see cref="TeacherAssignment"/> instead.
    /// </summary>
    public Guid? ClassRoomId { get; set; }

    public DateTime CreatedAt { get; set; }

    public ClassRoom? ClassRoom { get; set; }
}
