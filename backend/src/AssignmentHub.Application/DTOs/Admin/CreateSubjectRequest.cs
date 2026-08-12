namespace AssignmentHub.Application.DTOs.Admin;

/// <summary>Payload for <c>POST /api/admin/subjects</c>.</summary>
public sealed class CreateSubjectRequest
{
    public string Name { get; init; } = string.Empty;
}
