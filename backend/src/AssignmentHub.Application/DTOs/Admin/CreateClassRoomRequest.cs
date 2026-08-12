namespace AssignmentHub.Application.DTOs.Admin;

/// <summary>Payload for <c>POST /api/admin/classrooms</c>.</summary>
public sealed class CreateClassRoomRequest
{
    public string Name { get; init; } = string.Empty;
}
