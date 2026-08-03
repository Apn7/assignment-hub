namespace AssignmentHub.Domain.Entities;

/// <summary>
/// Grants a teacher the right to teach one subject to one class. This is the row
/// that resource-level authorization checks against: a teacher may only act on
/// assignments whose (ClassRoomId, SubjectId) pair they hold here.
/// </summary>
public class TeacherAssignment
{
    public Guid Id { get; set; }

    /// <summary>References a <see cref="User"/> with <c>UserRole.Teacher</c>.</summary>
    public Guid TeacherId { get; set; }

    public Guid ClassRoomId { get; set; }

    public Guid SubjectId { get; set; }

    public User Teacher { get; set; } = null!;

    public ClassRoom ClassRoom { get; set; } = null!;

    public Subject Subject { get; set; } = null!;
}
