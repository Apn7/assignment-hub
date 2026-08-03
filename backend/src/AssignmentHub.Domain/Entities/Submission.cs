using AssignmentHub.Domain.Enums;

namespace AssignmentHub.Domain.Entities;

/// <summary>
/// One student's answer to one assignment. A student has at most one submission
/// per assignment, enforced by a unique index on (AssignmentId, StudentId).
/// </summary>
public class Submission
{
    public Guid Id { get; set; }

    public Guid AssignmentId { get; set; }

    /// <summary>References a <see cref="User"/> with <c>UserRole.Student</c>.</summary>
    public Guid StudentId { get; set; }

    public string AnswerText { get; set; } = string.Empty;

    /// <summary>UTC instant of the original submission.</summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>UTC instant of the most recent edit; equals SubmittedAt when untouched.</summary>
    public DateTime UpdatedAt { get; set; }

    public SubmissionStatus Status { get; set; }

    /// <summary>Null until reviewed. Must not exceed the assignment's MaxMarks.</summary>
    public int? Marks { get; set; }

    /// <summary>Teacher's comments. Null until reviewed.</summary>
    public string? Feedback { get; set; }

    /// <summary>UTC instant marks and feedback were recorded. Null until reviewed.</summary>
    public DateTime? ReviewedAt { get; set; }

    public Assignment Assignment { get; set; } = null!;

    public User Student { get; set; } = null!;
}
