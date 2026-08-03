namespace AssignmentHub.Application.DTOs.Submissions;

/// <summary>
/// Body of <c>PUT /api/assignments/{assignmentId}/submissions/mine</c>.
/// </summary>
/// <remarks>
/// The answer is the only thing a student may change. Status, marks and feedback
/// are the teacher's to set, and their absence here is what makes that true rather
/// than a rule that has to be remembered.
/// </remarks>
public sealed class UpdateSubmissionRequest : ISubmissionAnswerRequest
{
    public string AnswerText { get; init; } = string.Empty;
}
