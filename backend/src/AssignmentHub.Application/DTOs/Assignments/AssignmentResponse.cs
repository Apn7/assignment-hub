using AssignmentHub.Domain.Entities;

namespace AssignmentHub.Application.DTOs.Assignments;

/// <summary>
/// An assignment as returned to any role. Carries the class, subject and teacher
/// names alongside their ids so a client can render a list without a second
/// round trip per row.
/// </summary>
public sealed class AssignmentResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public Guid ClassRoomId { get; init; }

    public string ClassRoomName { get; init; } = string.Empty;

    public Guid SubjectId { get; init; }

    public string SubjectName { get; init; } = string.Empty;

    public Guid CreatedByTeacherId { get; init; }

    public string CreatedByTeacherName { get; init; } = string.Empty;

    /// <summary>UTC.</summary>
    public DateTime Deadline { get; init; }

    public int MaxMarks { get; init; }

    /// <summary>Status name, e.g. "Draft".</summary>
    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Projects an assignment loaded with its class, subject and teacher.
    /// </summary>
    /// <remarks>
    /// The navigation properties are read defensively: every repository method
    /// that feeds this includes them, and a name that came back empty would mean
    /// a missing <c>Include</c> rather than missing data. Failing soft here beats
    /// a 500 on a display field.
    /// </remarks>
    public static AssignmentResponse FromAssignment(Assignment assignment) => new()
    {
        Id = assignment.Id,
        Title = assignment.Title,
        Description = assignment.Description,
        ClassRoomId = assignment.ClassRoomId,
        ClassRoomName = assignment.ClassRoom?.Name ?? string.Empty,
        SubjectId = assignment.SubjectId,
        SubjectName = assignment.Subject?.Name ?? string.Empty,
        CreatedByTeacherId = assignment.CreatedByTeacherId,
        CreatedByTeacherName = assignment.CreatedByTeacher?.FullName ?? string.Empty,
        Deadline = assignment.Deadline,
        MaxMarks = assignment.MaxMarks,
        Status = assignment.Status.ToString(),
        CreatedAt = assignment.CreatedAt,
        UpdatedAt = assignment.UpdatedAt
    };
}
