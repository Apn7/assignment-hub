using AssignmentHub.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentHub.Api.Controllers;

/// <summary>
/// Liveness probe. Deliberately anonymous so the frontend can verify
/// connectivity before a user has logged in.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/health")]
[Produces("application/json")]
public sealed class HealthController : ControllerBase
{
    private readonly IHostEnvironment _environment;

    public HealthController(IHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>Reports that the API process is up and serving requests.</summary>
    /// <response code="200">The API is healthy.</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse
        {
            Status = "Healthy",
            Environment = _environment.EnvironmentName,
            TimestampUtc = DateTimeOffset.UtcNow
        });
    }
}
