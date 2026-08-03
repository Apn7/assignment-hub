namespace AssignmentHub.Application.DTOs.Assignments;

/// <summary>
/// Body of <c>POST /api/assignments</c>. The created assignment always starts as
/// a draft, so there is nothing here to say otherwise.
/// </summary>
public sealed class CreateAssignmentRequest : IAssignmentWriteRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public Guid ClassRoomId { get; init; }

    public Guid SubjectId { get; init; }

    /// <summary>
    /// UTC deadline. May be in the past on a draft — the future-deadline rule is
    /// enforced at publish time, not while drafting.
    /// </summary>
    public DateTime Deadline { get; init; }

    public int MaxMarks { get; init; }
}
