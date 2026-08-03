using AssignmentHub.Api.Contracts;
using AssignmentHub.Application.DTOs.Assignments;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentHub.Api.Controllers;

/// <summary>
/// Teacher-authored and student-visible assignment endpoints. Every action is one
/// line: the role gate is the attribute, and every other decision belongs to
/// <see cref="IAssignmentService"/>.
/// </summary>
/// <remarks>
/// Teachers and students share the route prefix but not a single action. A teacher
/// listing their own work and a student listing their class's work are different
/// queries with different visibility rules, so they are different endpoints rather
/// than one endpoint that behaves differently depending on who asks.
/// </remarks>
[Route("api/assignments")]
public sealed class AssignmentsController : ApiControllerBase
{
    private readonly IAssignmentService _assignments;

    public AssignmentsController(IAssignmentService assignments)
    {
        _assignments = assignments;
    }

    /// <summary>Creates an assignment as a draft.</summary>
    /// <response code="201">Draft created.</response>
    /// <response code="400">The body failed validation.</response>
    /// <response code="403">The caller is not assigned to this class and subject.</response>
    /// <remarks>
    /// No <c>Location</c> header: the only by-id endpoint is the student view, and
    /// pointing the creating teacher at a route that would refuse them would be
    /// worse than omitting it.
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Teacher))]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AssignmentResponse>> Create(
        [FromBody] CreateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assignments.CreateAsync(CurrentUserId, request, cancellationToken);

        return ToActionResult(result, StatusCodes.Status201Created);
    }

    /// <summary>Updates an assignment the calling teacher owns.</summary>
    /// <response code="200">Updated.</response>
    /// <response code="400">The body failed validation.</response>
    /// <response code="403">A draft was re-pointed at a class/subject the caller does not teach.</response>
    /// <response code="404">No such assignment, or it belongs to another teacher.</response>
    /// <response code="409">The assignment's current state forbids this change.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Teacher))]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssignmentResponse>> Update(
        Guid id,
        [FromBody] UpdateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assignments.UpdateAsync(CurrentUserId, id, request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>Publishes a draft, making it visible to the target class.</summary>
    /// <response code="200">Published.</response>
    /// <response code="404">No such assignment, or it belongs to another teacher.</response>
    /// <response code="409">Already published, or its deadline is not in the future.</response>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = nameof(UserRole.Teacher))]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssignmentResponse>> Publish(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _assignments.PublishAsync(CurrentUserId, id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>Deletes a draft the calling teacher owns.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="404">No such assignment, or it belongs to another teacher.</response>
    /// <response code="409">The assignment is published and cannot be deleted.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Teacher))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _assignments.DeleteAsync(CurrentUserId, id, cancellationToken);

        return ToStatusResult(result);
    }

    /// <summary>The calling teacher's own assignments, drafts included.</summary>
    /// <response code="200">Assignments, newest first.</response>
    [HttpGet("mine")]
    [Authorize(Roles = nameof(UserRole.Teacher))]
    [ProducesResponseType(typeof(IReadOnlyList<AssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssignmentResponse>>> Mine(
        [FromQuery] AssignmentFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _assignments.ListForTeacherAsync(CurrentUserId, filter, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>Published assignments for the calling student's own class.</summary>
    /// <response code="200">Assignments, nearest deadline first.</response>
    /// <remarks>
    /// Takes no class parameter. The class is read from the student's stored record,
    /// so there is nothing here to tamper with.
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Student))]
    [ProducesResponseType(typeof(IReadOnlyList<AssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssignmentResponse>>> ForMyClass(
        CancellationToken cancellationToken)
    {
        var result = await _assignments.ListForStudentAsync(CurrentUserId, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>One published assignment, if it targets the student's class.</summary>
    /// <response code="200">The assignment.</response>
    /// <response code="404">Not published, not this student's class, or no such id.</response>
    /// <remarks>
    /// A draft returns 404 rather than 403 on purpose: 403 would confirm that the id
    /// names something real, which is exactly what a student must not learn about
    /// unpublished work.
    /// </remarks>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Student))]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentResponse>> Detail(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _assignments.GetForStudentAsync(CurrentUserId, id, cancellationToken);

        return ToActionResult(result);
    }
}
