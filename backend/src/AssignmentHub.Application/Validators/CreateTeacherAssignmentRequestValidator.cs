using AssignmentHub.Application.DTOs.Admin;
using FluentValidation;

namespace AssignmentHub.Application.Validators;

public sealed class CreateTeacherAssignmentRequestValidator : AbstractValidator<CreateTeacherAssignmentRequest>
{
    public CreateTeacherAssignmentRequestValidator()
    {
        RuleFor(request => request.TeacherId)
            .NotEmpty().WithMessage("Teacher is required.");

        RuleFor(request => request.ClassRoomId)
            .NotEmpty().WithMessage("Class is required.");

        RuleFor(request => request.SubjectId)
            .NotEmpty().WithMessage("Subject is required.");
    }
}
