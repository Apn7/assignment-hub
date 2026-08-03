using AssignmentHub.Application.DTOs.Submissions;
using FluentValidation;

namespace AssignmentHub.Application.Validators;

public sealed class ChangeSubmissionStatusRequestValidator
    : AbstractValidator<ChangeSubmissionStatusRequest>
{
    public ChangeSubmissionStatusRequestValidator()
    {
        // A name that is not a member fails during JSON binding, but a raw number
        // does not: `{"status": 99}` deserialises happily into an undefined enum
        // value. IsInEnum is what stops that reaching the database.
        RuleFor(request => request.Status)
            .IsInEnum().WithMessage("Status must be either Submitted or Reviewed.");
    }
}
