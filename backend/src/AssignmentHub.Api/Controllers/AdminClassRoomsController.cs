using AssignmentHub.Application.DTOs.Admin;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentHub.Api.Controllers;

/// <summary>
/// Admin management of classrooms.
/// </summary>
[Route("api/admin/classrooms")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminClassRoomsController : ApiControllerBase
{
    private readonly IAdminManagementService _admin;

    public AdminClassRoomsController(IAdminManagementService admin)
    {
        _admin = admin;
    }

    /// <summary>Create a new classroom.</summary>
    /// <response code="201">Classroom created.</response>
    /// <response code="409">Name already taken.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClassRoomResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassRoomResponse>> Create(
        [FromBody] CreateClassRoomRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _admin.CreateClassRoomAsync(request, cancellationToken);

        return ToActionResult(result, StatusCodes.Status201Created);
    }

    /// <summary>List all classrooms.</summary>
    /// <response code="200">Classrooms list.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClassRoomResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClassRoomResponse>>> List(
        CancellationToken cancellationToken)
    {
        var result = await _admin.ListClassRoomsAsync(cancellationToken);

        return ToActionResult(result);
    }
}
