namespace AssignmentHub.Application.DTOs.Admin;

/// <summary>Payload for <c>POST /api/admin/teacher-assignments</c>.</summary>
public sealed class CreateTeacherAssignmentRequest
{
    public Guid TeacherId { get; init; }

    public Guid ClassRoomId { get; init; }

    public Guid SubjectId { get; init; }
}
