using AssignmentHub.Application.DTOs.Assignments;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentHub.Api.Controllers;

/// <summary>
/// The admin's read-only view of every assignment in the system.
/// </summary>
/// <remarks>
/// A separate controller under <c>/api/admin</c> rather than a role check inside
/// the teacher/student controller. The route says who it is for, the role gate sits
/// on the class so no future action can be added without it, and an admin listing
/// everything is a different query from a teacher listing their own work.
/// </remarks>
[Route("api/admin/assignments")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminAssignmentsController : ApiControllerBase
{
    private readonly IAssignmentService _assignments;

    public AdminAssignmentsController(IAssignmentService assignments)
    {
        _assignments = assignments;
    }

    /// <summary>Every assignment, drafts included, optionally filtered.</summary>
    /// <response code="200">Assignments, newest first.</response>
    /// <response code="401">Token missing or invalid.</response>
    /// <response code="403">Authenticated, but not an Admin.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AssignmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AssignmentResponse>>> List(
        [FromQuery] AssignmentFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _assignments.ListAllAsync(filter, cancellationToken);

        return ToActionResult(result);
    }
}
