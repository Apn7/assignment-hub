using AssignmentHub.Domain.Enums;

namespace AssignmentHub.Application.DTOs.Assignments;

/// <summary>
/// Optional filters for the teacher and admin listings. Every field is
/// independent, and a null field means "no restriction on this dimension".
/// </summary>
/// <remarks>
/// Mutable properties rather than <c>init</c> because this is bound from the
/// query string by MVC's complex-object binder, not deserialised from a body.
/// This is a narrowing filter only — it can never widen what the caller is
/// allowed to see, which is why the same type is safe for both roles.
/// </remarks>
public sealed class AssignmentFilter
{
    public Guid? ClassRoomId { get; set; }

    public Guid? SubjectId { get; set; }

    /// <summary><c>Draft</c> or <c>Published</c>.</summary>
    public AssignmentStatus? Status { get; set; }

    /// <summary>An unrestricted filter, for callers that have nothing to narrow.</summary>
    public static AssignmentFilter None => new();
}
