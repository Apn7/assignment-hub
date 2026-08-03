using AssignmentHub.Domain.Enums;

namespace AssignmentHub.Application.DTOs.Submissions;

/// <summary>
/// Optional filters for the admin submission listing. Each field is independent,
/// and a null field places no restriction on that dimension.
/// </summary>
/// <remarks>
/// Mutable properties because this is bound from the query string, not
/// deserialised from a body. Narrowing only — it can never widen what the caller
/// is allowed to see.
/// </remarks>
public sealed class SubmissionFilter
{
    public Guid? AssignmentId { get; set; }

    /// <summary>Matches on the parent assignment's class.</summary>
    public Guid? ClassRoomId { get; set; }

    /// <summary><c>Submitted</c> or <c>Reviewed</c>.</summary>
    public SubmissionStatus? Status { get; set; }

    /// <summary>An unrestricted filter, for callers with nothing to narrow.</summary>
    public static SubmissionFilter None => new();
}
