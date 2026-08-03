using AssignmentHub.Api.Contracts;
using AssignmentHub.Application.DTOs.Submissions;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentHub.Api.Controllers;

/// <summary>
/// A teacher's actions on one identified submission: read it, mark it, or move its
/// status.
/// </summary>
/// <remarks>
/// Flat rather than nested under the assignment. A submission id is globally
/// unique, so requiring the assignment id in the route would add a segment the
/// server would have to either verify or ignore — and either choice is worse than
/// not asking for it. Ownership is settled through the submission's own parent
/// assignment.
/// </remarks>
[Route("api/submissions")]
[Authorize(Roles = nameof(UserRole.Teacher))]
public sealed class SubmissionsController : ApiControllerBase
{
    private readonly ISubmissionService _submissions;

    public SubmissionsController(ISubmissionService submissions)
    {
        _submissions = submissions;
    }

    /// <summary>One submission in full, if it sits on the caller's own assignment.</summary>
    /// <response code="200">The submission.</response>
    /// <response code="404">No such submission, or it is on another teacher's assignment.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionResponse>> Detail(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _submissions.GetForTeacherAsync(CurrentUserId, id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>Records marks and feedback, moving the submission to Reviewed.</summary>
    /// <response code="200">Graded.</response>
    /// <response code="400">The body failed validation.</response>
    /// <response code="404">No such submission, or it is on another teacher's assignment.</response>
    /// <response code="422">The marks fall outside 0..MaxMarks for this assignment.</response>
    /// <remarks>
    /// Re-grading an already reviewed submission is allowed and simply replaces the
    /// verdict — marking mistakes happen, and a correction should not require a
    /// database edit.
    /// </remarks>
    [HttpPost("{id:guid}/grade")]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SubmissionResponse>> Grade(
        Guid id,
        [FromBody] GradeSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _submissions.GradeAsync(CurrentUserId, id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>Sets the submission's status, leaving marks and feedback alone.</summary>
    /// <response code="200">Status set.</response>
    /// <response code="400">The body failed validation.</response>
    /// <response code="404">No such submission, or it is on another teacher's assignment.</response>
    /// <remarks>
    /// The requirement's "change the submission status when necessary". Its practical
    /// use is reopening: <c>Reviewed</c> back to <c>Submitted</c> so a student can
    /// revise. Marks survive that — see docs/submissions.md.
    /// </remarks>
    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionResponse>> ChangeStatus(
        Guid id,
        [FromBody] ChangeSubmissionStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _submissions.ChangeStatusAsync(CurrentUserId, id, request, cancellationToken);

        return ToActionResult(result);
    }
}
