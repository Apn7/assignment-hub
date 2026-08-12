using AssignmentHub.Application.DTOs.Admin;
using FluentValidation;

namespace AssignmentHub.Application.Validators;

public sealed class CreateSubjectRequestValidator : AbstractValidator<CreateSubjectRequest>
{
    public CreateSubjectRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Subject name is required.")
            .MaximumLength(100).WithMessage("Subject name must not exceed 100 characters.");
    }
}
