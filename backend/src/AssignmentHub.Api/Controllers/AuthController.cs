using AssignmentHub.Api.Contracts;
using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Auth;
using AssignmentHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentHub.Api.Controllers;

/// <summary>
/// Login and identity endpoints. Deliberately thin: every decision lives in
/// <see cref="IAuthService"/> so it can be unit-tested without HTTP.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Exchanges email and password for a JWT access token.</summary>
    /// <response code="200">Credentials accepted.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="401">Credentials rejected.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);

        if (result is null)
        {
            // One message for every failure. An unknown email and a wrong password
            // are indistinguishable to the caller, so this endpoint cannot be used
            // to discover which addresses have accounts.
            return Unauthorized(new ApiErrorResponse
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid email or password.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(result);
    }

    /// <summary>Returns the caller's identity, read from the token's claims.</summary>
    /// <response code="200">Token is valid.</response>
    /// <response code="401">Token missing, expired or invalid.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserSummary> Me()
    {
        // Read straight from the principal rather than the database: it proves the
        // token round-trip carried everything needed, and costs no query.
        return Ok(new UserSummary
        {
            Id = Guid.TryParse(User.FindFirst(AppClaimTypes.UserId)?.Value, out var id)
                ? id
                : Guid.Empty,
            FullName = User.FindFirst(AppClaimTypes.Name)?.Value ?? string.Empty,
            Email = User.FindFirst(AppClaimTypes.Email)?.Value ?? string.Empty,
            Role = User.FindFirst(AppClaimTypes.Role)?.Value ?? string.Empty
        });
    }
}
