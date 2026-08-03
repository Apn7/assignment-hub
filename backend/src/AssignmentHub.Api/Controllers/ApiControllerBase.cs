using AssignmentHub.Api.Contracts;
using AssignmentHub.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentHub.Api.Controllers;

/// <summary>
/// Shared plumbing for controllers that delegate to an Application service: who
/// is calling, and how a <see cref="Result"/> becomes an HTTP response.
/// </summary>
/// <remarks>
/// The status-code mapping lives here and nowhere else, which is what makes the
/// 403-versus-404 distinction a property of the system rather than a habit each
/// action has to remember.
/// </remarks>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// The authenticated caller's id, from the token's <c>sub</c> claim.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The claim is absent or unparsable. Only reachable on an <c>[Authorize]</c>d
    /// action, so it means the token contract and the validation configuration have
    /// diverged — a server fault, and it should surface as one rather than quietly
    /// becoming <see cref="Guid.Empty"/> and matching nothing.
    /// </exception>
    protected Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst(AppClaimTypes.UserId)?.Value, out var id)
            ? id
            : throw new InvalidOperationException(
                $"Authenticated request carried no usable '{AppClaimTypes.UserId}' claim.");

    /// <summary>Maps a value-carrying result onto a response.</summary>
    protected ActionResult<T> ToActionResult<T>(
        Result<T> result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        return result.IsSuccess
            ? StatusCode(successStatusCode, result.Value)
            : ErrorResponse(result);
    }

    /// <summary>Maps a result with no value; success defaults to 204.</summary>
    protected IActionResult ToStatusResult(
        Result result,
        int successStatusCode = StatusCodes.Status204NoContent)
    {
        return result.IsSuccess
            ? StatusCode(successStatusCode)
            : ErrorResponse(result);
    }

    private ObjectResult ErrorResponse(Result result)
    {
        var statusCode = result.Status switch
        {
            // The request itself is wrong, independent of any stored state.
            ResultStatus.ValidationFailed => StatusCodes.Status400BadRequest,
            // The caller has no standing over this class/subject at all.
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            // Absent, or not theirs — deliberately the same answer.
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            // Well-formed, but the stored state forbids the transition.
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            // Well-formed and permitted, but a value is out of range for the
            // resource it targets — 12 marks on an assignment worth 10.
            ResultStatus.Unprocessable => StatusCodes.Status422UnprocessableEntity,
            _ => throw new InvalidOperationException(
                $"{result.Status} is not a failure status and has no HTTP mapping.")
        };

        return StatusCode(statusCode, new ApiErrorResponse
        {
            Status = statusCode,
            Title = result.Error ?? "The request could not be completed.",
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
