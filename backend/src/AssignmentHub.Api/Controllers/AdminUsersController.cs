using AssignmentHub.Application.DTOs.Admin;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentHub.Api.Controllers;

/// <summary>
/// Admin management of user accounts.
/// </summary>
[Route("api/admin/users")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminUsersController : ApiControllerBase
{
    private readonly IAdminManagementService _admin;

    public AdminUsersController(IAdminManagementService admin)
    {
        _admin = admin;
    }

    /// <summary>Create a new user.</summary>
    /// <response code="201">User created.</response>
    /// <response code="409">Email already taken.</response>
    /// <response code="422">Role/class combination invalid.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _admin.CreateUserAsync(request, cancellationToken);

        return ToActionResult(result, StatusCodes.Status201Created);
    }

    /// <summary>List all users, optionally filtered by role.</summary>
    /// <response code="200">Users list.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(
        [FromQuery] string? role,
        CancellationToken cancellationToken)
    {
        UserRole? roleFilter = null;
        if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed))
        {
            roleFilter = parsed;
        }

        var result = await _admin.ListUsersAsync(roleFilter, cancellationToken);

        return ToActionResult(result);
    }
}
