using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Submissions;
using FluentValidation;

namespace AssignmentHub.Application.Validators;

/// <summary>
/// Rule 7, shared by submitting and revising: an answer must be present and of
/// sane length.
/// </summary>
/// <remarks>
/// State-dependent rules — the deadline, whether the work is already reviewed —
/// need the stored row and live in <c>SubmissionService</c>.
/// </remarks>
public abstract class SubmissionAnswerRequestValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : ISubmissionAnswerRequest
{
    protected SubmissionAnswerRequestValidator()
    {
        // NotEmpty rejects whitespace-only text, so a student cannot "submit" a
        // spacebar and claim to have handed something in.
        RuleFor(request => request.AnswerText)
            .NotEmpty().WithMessage("An answer is required.")
            .MaximumLength(SubmissionRules.AnswerMaxLength)
            .WithMessage($"An answer must not exceed {SubmissionRules.AnswerMaxLength} characters.");
    }
}

public sealed class SubmitAnswerRequestValidator
    : SubmissionAnswerRequestValidator<SubmitAnswerRequest>
{
}

public sealed class UpdateSubmissionRequestValidator
    : SubmissionAnswerRequestValidator<UpdateSubmissionRequest>
{
}
