namespace AssignmentHub.Application.DTOs.Admin;

/// <summary>Response shape for classroom listings.</summary>
public sealed class ClassRoomResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;
}
