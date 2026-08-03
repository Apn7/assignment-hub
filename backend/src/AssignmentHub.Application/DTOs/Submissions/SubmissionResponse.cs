using AssignmentHub.Domain.Entities;

namespace AssignmentHub.Application.DTOs.Submissions;

/// <summary>
/// One submission in full, as returned to the student who owns it and to the
/// teacher who set the assignment.
/// </summary>
/// <remarks>
/// Both roles get the same shape. There is nothing here a student may not see
/// about their own work, and nothing the owning teacher may not see about a
/// submission on their own assignment, so a second projection would add a type
/// without adding a rule.
/// </remarks>
public sealed class SubmissionResponse
{
    public Guid Id { get; init; }

    public Guid AssignmentId { get; init; }

    public string AssignmentTitle { get; init; } = string.Empty;

    public string ClassRoomName { get; init; } = string.Empty;

    public string SubjectName { get; init; } = string.Empty;

    /// <summary>The assignment's maximum, so a client can render "8 / 10".</summary>
    public int MaxMarks { get; init; }

    /// <summary>UTC. Copied from the assignment so a client need not fetch it.</summary>
    public DateTime Deadline { get; init; }

    public Guid StudentId { get; init; }

    public string StudentName { get; init; } = string.Empty;

    public string AnswerText { get; init; } = string.Empty;

    /// <summary>UTC instant of the original submission.</summary>
    public DateTime SubmittedAt { get; init; }

    /// <summary>UTC instant of the student's most recent edit.</summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>Status name, e.g. "Submitted".</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Null until graded.</summary>
    public int? Marks { get; init; }

    /// <summary>Null until graded, and may stay null if the teacher gave none.</summary>
    public string? Feedback { get; init; }

    /// <summary>UTC instant marks were last recorded. Null until graded.</summary>
    public DateTime? ReviewedAt { get; init; }

    /// <summary>
    /// Projects a submission loaded with its student and its assignment.
    /// </summary>
    /// <remarks>
    /// Navigations are read defensively for the same reason as
    /// <c>AssignmentResponse</c>: every repository method that feeds this includes
    /// them, and an empty name would mean a missing <c>Include</c> rather than
    /// missing data. Failing soft beats a 500 on a display field.
    /// </remarks>
    public static SubmissionResponse FromSubmission(Submission submission) => new()
    {
        Id = submission.Id,
        AssignmentId = submission.AssignmentId,
        AssignmentTitle = submission.Assignment?.Title ?? string.Empty,
        ClassRoomName = submission.Assignment?.ClassRoom?.Name ?? string.Empty,
        SubjectName = submission.Assignment?.Subject?.Name ?? string.Empty,
        MaxMarks = submission.Assignment?.MaxMarks ?? 0,
        Deadline = submission.Assignment?.Deadline ?? default,
        StudentId = submission.StudentId,
        StudentName = submission.Student?.FullName ?? string.Empty,
        AnswerText = submission.AnswerText,
        SubmittedAt = submission.SubmittedAt,
        UpdatedAt = submission.UpdatedAt,
        Status = submission.Status.ToString(),
        Marks = submission.Marks,
        Feedback = submission.Feedback,
        ReviewedAt = submission.ReviewedAt
    };
}
