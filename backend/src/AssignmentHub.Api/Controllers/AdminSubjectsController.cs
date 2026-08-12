using AssignmentHub.Application.DTOs.Admin;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentHub.Api.Controllers;

/// <summary>
/// Admin management of subjects.
/// </summary>
[Route("api/admin/subjects")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminSubjectsController : ApiControllerBase
{
    private readonly IAdminManagementService _admin;

    public AdminSubjectsController(IAdminManagementService admin)
    {
        _admin = admin;
    }

    /// <summary>Create a new subject.</summary>
    /// <response code="201">Subject created.</response>
    /// <response code="409">Name already taken.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SubjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubjectResponse>> Create(
        [FromBody] CreateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _admin.CreateSubjectAsync(request, cancellationToken);

        return ToActionResult(result, StatusCodes.Status201Created);
    }

    /// <summary>List all subjects.</summary>
    /// <response code="200">Subjects list.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SubjectResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubjectResponse>>> List(
        CancellationToken cancellationToken)
    {
        var result = await _admin.ListSubjectsAsync(cancellationToken);

        return ToActionResult(result);
    }
}
