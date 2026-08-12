using AssignmentHub.Application.DTOs.Admin;
using FluentValidation;

namespace AssignmentHub.Application.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    private static readonly string[] ValidRoles = { "Admin", "Teacher", "Student" };

    public CreateUserRequestValidator()
    {
        RuleFor(request => request.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(150).WithMessage("Full name must not exceed 150 characters.");

        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(request => request.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => ValidRoles.Contains(role))
            .WithMessage("Role must be Admin, Teacher, or Student.");
    }
}
