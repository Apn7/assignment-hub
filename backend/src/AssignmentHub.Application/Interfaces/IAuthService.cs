using AssignmentHub.Application.DTOs.Auth;

namespace AssignmentHub.Application.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Validates credentials and issues an access token.
    /// </summary>
    /// <returns>
    /// The token and user summary on success; <c>null</c> if authentication failed.
    /// </returns>
    /// <remarks>
    /// A single null result for every failure is deliberate. The signature simply
    /// cannot express "no such user" separately from "wrong password", so no
    /// caller — present or future — can leak the difference and turn the login
    /// endpoint into a user-enumeration oracle.
    /// </remarks>
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
