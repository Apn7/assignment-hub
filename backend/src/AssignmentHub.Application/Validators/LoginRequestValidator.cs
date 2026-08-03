using AssignmentHub.Application.DTOs.Auth;
using FluentValidation;

namespace AssignmentHub.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        // Presence only. Complexity rules belong on the path that *sets* a password;
        // enforcing them here would reject a legitimate attempt against an older
        // password and would advertise the policy to anyone probing the endpoint.
        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
