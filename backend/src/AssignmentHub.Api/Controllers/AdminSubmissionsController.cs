using AssignmentHub.Application.DTOs.Submissions;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentHub.Api.Controllers;

/// <summary>
/// The admin's read-only view of every submission in the system.
/// </summary>
/// <remarks>
/// Separate controller under <c>/api/admin</c> for the same reasons as
/// <see cref="AdminAssignmentsController"/>: the route says who it is for, and the
/// role gate sits on the class so no action can be added without it.
/// </remarks>
[Route("api/admin/submissions")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminSubmissionsController : ApiControllerBase
{
    private readonly ISubmissionService _submissions;

    public AdminSubmissionsController(ISubmissionService submissions)
    {
        _submissions = submissions;
    }

    /// <summary>Every submission, optionally filtered.</summary>
    /// <response code="200">Submissions, most recent first.</response>
    /// <response code="401">Token missing or invalid.</response>
    /// <response code="403">Authenticated, but not an Admin.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SubmissionListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<SubmissionListItem>>> List(
        [FromQuery] SubmissionFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _submissions.ListAllAsync(filter, cancellationToken);

        return ToActionResult(result);
    }
}
