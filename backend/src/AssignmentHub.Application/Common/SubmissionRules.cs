namespace AssignmentHub.Application.Common;

/// <summary>
/// Limits shared by the submission request validators and
/// <c>SubmissionService</c>.
/// </summary>
public static class SubmissionRules
{
    /// <summary>
    /// Generous enough for a long essay answer, small enough that a runaway client
    /// cannot post a megabyte into a <c>text</c> column. There is no length cap in
    /// the database, so this is the only bound.
    /// </summary>
    public const int AnswerMaxLength = 20_000;

    /// <summary>Must match <c>SubmissionConfiguration</c>'s <c>HasMaxLength</c>.</summary>
    public const int FeedbackMaxLength = 2000;

    /// <summary>
    /// Marks are never negative. The upper bound is the assignment's own
    /// <c>MaxMarks</c>, so it cannot live here — see
    /// <c>SubmissionService.ValidateMarks</c>.
    /// </summary>
    public const int MinMarks = 0;
}
