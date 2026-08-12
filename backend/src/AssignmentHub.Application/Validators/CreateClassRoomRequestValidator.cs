using AssignmentHub.Application.DTOs.Admin;
using FluentValidation;

namespace AssignmentHub.Application.Validators;

public sealed class CreateClassRoomRequestValidator : AbstractValidator<CreateClassRoomRequest>
{
    public CreateClassRoomRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Class name is required.")
            .MaximumLength(100).WithMessage("Class name must not exceed 100 characters.");
    }
}
