using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Submissions;
using FluentValidation;

namespace AssignmentHub.Application.Validators;

/// <summary>
/// Shape rules for grading. Note what is <em>not</em> here.
/// </summary>
/// <remarks>
/// <c>Marks</c> is deliberately unvalidated. Its legal range runs from zero to the
/// assignment's own <c>MaxMarks</c>, which no request validator can see, so the
/// whole range check belongs to <c>SubmissionService</c> and comes back as a 422
/// naming the real maximum. Adding a partial rule here — rejecting negatives at
/// the edge — would mean −1 and 999 failed with different status codes for the
/// same reason, which is worse than checking neither.
/// </remarks>
public sealed class GradeSubmissionRequestValidator : AbstractValidator<GradeSubmissionRequest>
{
    public GradeSubmissionRequestValidator()
    {
        // Matches the column width. Optional: a mark with no comment is legitimate.
        RuleFor(request => request.Feedback)
            .MaximumLength(SubmissionRules.FeedbackMaxLength)
            .WithMessage($"Feedback must not exceed {SubmissionRules.FeedbackMaxLength} characters.");
    }
}
