namespace AssignmentHub.Application.DTOs.Submissions;

/// <summary>
/// The answer a student sends when submitting or revising. Submitting and revising
/// carry the same field but mean different things and obey different rules, so they
/// stay separate types; this interface exists only so their shared validation is
/// written once.
/// </summary>
public interface ISubmissionAnswerRequest
{
    string AnswerText { get; }
}
