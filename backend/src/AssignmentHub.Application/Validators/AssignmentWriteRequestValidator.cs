using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Assignments;
using FluentValidation;

namespace AssignmentHub.Application.Validators;

/// <summary>
/// Shape rules shared by create and update: the checks that can be made without
/// knowing anything about the stored assignment.
/// </summary>
/// <remarks>
/// State-dependent rules — what a published assignment may still change, whether
/// a deadline is in the future — are not here. They need the current row, so they
/// live in <c>AssignmentService</c> where they are unit-testable.
/// </remarks>
public abstract class AssignmentWriteRequestValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : IAssignmentWriteRequest
{
    protected AssignmentWriteRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(AssignmentRules.TitleMaxLength)
            .WithMessage($"Title must not exceed {AssignmentRules.TitleMaxLength} characters.");

        RuleFor(request => request.Description)
            .NotEmpty().WithMessage("Description is required.");

        // NotEmpty rejects Guid.Empty, which is what an unselected dropdown sends.
        RuleFor(request => request.ClassRoomId)
            .NotEmpty().WithMessage("A class must be selected.");

        RuleFor(request => request.SubjectId)
            .NotEmpty().WithMessage("A subject must be selected.");

        // A missing or unparsable date binds to default(DateTime), not to null.
        RuleFor(request => request.Deadline)
            .NotEmpty().WithMessage("A deadline is required.");

        // Mirrors AssignmentService.ValidateMaxMarks. Duplicated on purpose: this
        // gives the client a field-level 400, while the service keeps the rule true
        // for any caller. Both read the same constants.
        RuleFor(request => request.MaxMarks)
            .GreaterThanOrEqualTo(AssignmentRules.MinMaxMarks)
            .WithMessage("Maximum marks must be greater than zero.")
            .LessThanOrEqualTo(AssignmentRules.MaxAllowedMarks)
            .WithMessage($"Maximum marks must not exceed {AssignmentRules.MaxAllowedMarks}.");
    }
}

public sealed class CreateAssignmentRequestValidator
    : AssignmentWriteRequestValidator<CreateAssignmentRequest>
{
}

public sealed class UpdateAssignmentRequestValidator
    : AssignmentWriteRequestValidator<UpdateAssignmentRequest>
{
}
