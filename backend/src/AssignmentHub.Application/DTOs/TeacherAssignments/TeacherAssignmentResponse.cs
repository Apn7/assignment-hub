namespace AssignmentHub.Application.DTOs.TeacherAssignments;

/// <summary>
/// A class/subject teaching pair assigned to a teacher.
/// </summary>
public sealed class TeacherAssignmentResponse
{
    public Guid ClassRoomId { get; init; }

    public string ClassRoomName { get; init; } = string.Empty;

    public Guid SubjectId { get; init; }

    public string SubjectName { get; init; } = string.Empty;
}
