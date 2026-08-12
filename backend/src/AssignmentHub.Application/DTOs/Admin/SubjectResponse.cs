namespace AssignmentHub.Application.DTOs.Admin;

/// <summary>Response shape for subject listings.</summary>
public sealed class SubjectResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;
}
