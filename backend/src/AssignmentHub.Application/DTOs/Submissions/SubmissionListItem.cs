using AssignmentHub.Domain.Entities;

namespace AssignmentHub.Application.DTOs.Submissions;

/// <summary>
/// A submission as it appears in a list: who, when, where it stands, what it
/// scored.
/// </summary>
/// <remarks>
/// Deliberately omits <c>AnswerText</c> and <c>Feedback</c>. An answer may run to
/// twenty thousand characters, so a class of thirty would make a marking overview
/// megabytes long for two fields nobody reads at that zoom level. The full text is
/// one request away at <c>GET /api/submissions/{id}</c>.
/// </remarks>
public sealed class SubmissionListItem
{
    public Guid Id { get; init; }

    public Guid AssignmentId { get; init; }

    public string AssignmentTitle { get; init; } = string.Empty;

    public string ClassRoomName { get; init; } = string.Empty;

    public Guid StudentId { get; init; }

    public string StudentName { get; init; } = string.Empty;

    /// <summary>UTC.</summary>
    public DateTime SubmittedAt { get; init; }

    /// <summary>UTC instant of the student's most recent edit.</summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>Status name, e.g. "Reviewed".</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Null until graded.</summary>
    public int? Marks { get; init; }

    /// <summary>The assignment's maximum, so a list can render "8 / 10".</summary>
    public int MaxMarks { get; init; }

    public static SubmissionListItem FromSubmission(Submission submission) => new()
    {
        Id = submission.Id,
        AssignmentId = submission.AssignmentId,
        AssignmentTitle = submission.Assignment?.Title ?? string.Empty,
        ClassRoomName = submission.Assignment?.ClassRoom?.Name ?? string.Empty,
        StudentId = submission.StudentId,
        StudentName = submission.Student?.FullName ?? string.Empty,
        SubmittedAt = submission.SubmittedAt,
        UpdatedAt = submission.UpdatedAt,
        Status = submission.Status.ToString(),
        Marks = submission.Marks,
        MaxMarks = submission.Assignment?.MaxMarks ?? 0
    };
}
