using AssignmentHub.Application.DTOs.Admin;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentHub.Api.Controllers;

/// <summary>
/// Admin management of teacher-assignment entitlements (teacher–class–subject mappings).
/// </summary>
[Route("api/admin/teacher-assignments")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminTeacherAssignmentsController : ApiControllerBase
{
    private readonly IAdminManagementService _admin;

    public AdminTeacherAssignmentsController(IAdminManagementService admin)
    {
        _admin = admin;
    }

    /// <summary>Create a teaching entitlement.</summary>
    /// <response code="201">Entitlement created.</response>
    /// <response code="409">Duplicate teacher/class/subject triple.</response>
    /// <response code="422">Non-existent IDs or target is not a Teacher.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TeacherAssignmentAdminResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TeacherAssignmentAdminResponse>> Create(
        [FromBody] CreateTeacherAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _admin.CreateTeacherAssignmentAsync(request, cancellationToken);

        return ToActionResult(result, StatusCodes.Status201Created);
    }

    /// <summary>List all teaching entitlements with resolved names.</summary>
    /// <response code="200">Entitlements list.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TeacherAssignmentAdminResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeacherAssignmentAdminResponse>>> List(
        CancellationToken cancellationToken)
    {
        var result = await _admin.ListTeacherAssignmentsAsync(cancellationToken);

        return ToActionResult(result);
    }
}
