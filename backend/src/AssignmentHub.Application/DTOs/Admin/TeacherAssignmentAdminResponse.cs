namespace AssignmentHub.Application.DTOs.Admin;

/// <summary>
/// Admin-view of a teacher–class–subject entitlement, including resolved names.
/// </summary>
public sealed class TeacherAssignmentAdminResponse
{
    public Guid Id { get; init; }

    public Guid TeacherId { get; init; }

    public string TeacherName { get; init; } = string.Empty;

    public Guid ClassRoomId { get; init; }

    public string ClassRoomName { get; init; } = string.Empty;

    public Guid SubjectId { get; init; }

    public string SubjectName { get; init; } = string.Empty;
}
