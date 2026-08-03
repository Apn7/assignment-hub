using AssignmentHub.Api.Contracts;
using AssignmentHub.Application.DTOs.Submissions;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentHub.Api.Controllers;

/// <summary>
/// Submissions in the context of one assignment: what a student hands in, and the
/// marking list the owning teacher works from.
/// </summary>
/// <remarks>
/// Nested under the assignment because that is the only way a student reaches a
/// submission — they have no submission id to hold, only "my answer to this
/// assignment". The teacher's per-submission actions take an id and live on
/// <see cref="SubmissionsController"/>.
/// </remarks>
[Route("api/assignments/{assignmentId:guid}/submissions")]
public sealed class AssignmentSubmissionsController : ApiControllerBase
{
    private readonly ISubmissionService _submissions;

    public AssignmentSubmissionsController(ISubmissionService submissions)
    {
        _submissions = submissions;
    }

    /// <summary>Submits the calling student's answer.</summary>
    /// <response code="201">Answer recorded.</response>
    /// <response code="400">The body failed validation.</response>
    /// <response code="404">The assignment is not published for this student's class.</response>
    /// <response code="409">The deadline has passed, or an answer already exists.</response>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Student))]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmissionResponse>> Submit(
        Guid assignmentId,
        [FromBody] SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _submissions.SubmitAsync(
            CurrentUserId, assignmentId, request, cancellationToken);

        return ToActionResult(result, StatusCodes.Status201Created);
    }

    /// <summary>Revises the calling student's own answer.</summary>
    /// <response code="200">Answer updated.</response>
    /// <response code="400">The body failed validation.</response>
    /// <response code="404">This student has not submitted to this assignment.</response>
    /// <response code="409">The deadline has passed, or the work has been reviewed.</response>
    /// <remarks>
    /// Addressed as <c>mine</c> rather than by id. A student never needs to name a
    /// submission — they have exactly one per assignment — and a route that took an
    /// id would invite an ownership check that this one cannot forget.
    /// </remarks>
    [HttpPut("mine")]
    [Authorize(Roles = nameof(UserRole.Student))]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmissionResponse>> UpdateMine(
        Guid assignmentId,
        [FromBody] UpdateSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _submissions.UpdateOwnAsync(
            CurrentUserId, assignmentId, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// The calling student's own submission, with status, marks and feedback.
    /// </summary>
    /// <response code="200">The submission. Marks and feedback are null until graded.</response>
    /// <response code="404">This student has not submitted to this assignment.</response>
    [HttpGet("mine")]
    [Authorize(Roles = nameof(UserRole.Student))]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionResponse>> Mine(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var result = await _submissions.GetOwnAsync(CurrentUserId, assignmentId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>Every submission on the calling teacher's own assignment.</summary>
    /// <response code="200">Submissions, earliest first.</response>
    /// <response code="404">No such assignment, or it belongs to another teacher.</response>
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Teacher))]
    [ProducesResponseType(typeof(IReadOnlyList<SubmissionListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SubmissionListItem>>> ForAssignment(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var result = await _submissions.ListForAssignmentAsync(
            CurrentUserId, assignmentId, cancellationToken);

        return ToActionResult(result);
    }
}
