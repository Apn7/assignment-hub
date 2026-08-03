namespace AssignmentHub.Application.DTOs.Assignments;

/// <summary>
/// Body of <c>PUT /api/assignments/{id}</c>. A full representation, not a patch:
/// every field is sent, and the service compares the incoming values against the
/// stored ones to decide whether a published assignment's immutable fields were
/// touched.
/// </summary>
/// <remarks>
/// Sending a field unchanged is therefore always accepted — a pre-filled edit
/// form that round-trips the current class, subject and marks is not an attempt
/// to change them.
/// </remarks>
public sealed class UpdateAssignmentRequest : IAssignmentWriteRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    /// <summary>Immutable once published; must equal the stored value.</summary>
    public Guid ClassRoomId { get; init; }

    /// <summary>Immutable once published; must equal the stored value.</summary>
    public Guid SubjectId { get; init; }

    /// <summary>UTC. Once published this may move later, never earlier.</summary>
    public DateTime Deadline { get; init; }

    /// <summary>Immutable once published; must equal the stored value.</summary>
    public int MaxMarks { get; init; }
}
