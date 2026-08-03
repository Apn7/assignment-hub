using AssignmentHub.Domain.Enums;

namespace AssignmentHub.Application.DTOs.Submissions;

/// <summary>
/// Body of <c>POST /api/submissions/{id}/status</c>, the endpoint behind the
/// requirement's "change the submission status when necessary".
/// </summary>
/// <remarks>
/// Its main use is reopening: moving a reviewed submission back to
/// <c>Submitted</c> so the student can revise it. Marks and feedback are left
/// alone — see docs/submissions.md.
/// </remarks>
public sealed class ChangeSubmissionStatusRequest
{
    /// <summary>Accepts the name (<c>"Submitted"</c>) or the numeric value.</summary>
    public SubmissionStatus Status { get; init; }
}
