using AssignmentHub.Api.Contracts;
using AssignmentHub.Application.DTOs.TeacherAssignments;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentHub.Api.Controllers;

/// <summary>
/// Endpoints for inspecting teacher class/subject assignments.
/// </summary>
[Route("api/teacher-assignments")]
public sealed class TeacherAssignmentsController : ApiControllerBase
{
    private readonly ITeacherAssignmentService _teacherAssignments;

    public TeacherAssignmentsController(ITeacherAssignmentService teacherAssignments)
    {
        _teacherAssignments = teacherAssignments;
    }

    /// <summary>The calling teacher's assigned class/subject pairs.</summary>
    /// <response code="200">The assigned pairs.</response>
    [HttpGet("mine")]
    [Authorize(Roles = nameof(UserRole.Teacher))]
    [ProducesResponseType(typeof(IReadOnlyList<TeacherAssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeacherAssignmentResponse>>> Mine(
        CancellationToken cancellationToken)
    {
        var result = await _teacherAssignments.ListMineAsync(CurrentUserId, cancellationToken);

        return ToActionResult(result);
    }
}
