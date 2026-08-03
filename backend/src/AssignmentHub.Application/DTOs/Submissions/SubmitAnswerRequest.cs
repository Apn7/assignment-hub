namespace AssignmentHub.Application.DTOs.Submissions;

/// <summary>
/// Body of <c>POST /api/assignments/{assignmentId}/submissions</c>.
/// </summary>
/// <remarks>
/// Carries no student id and no assignment status. The student is the caller, taken
/// from the token, and a submission always begins as <c>Submitted</c> — there is
/// nothing here with which to claim otherwise.
/// </remarks>
public sealed class SubmitAnswerRequest : ISubmissionAnswerRequest
{
    public string AnswerText { get; init; } = string.Empty;
}
