namespace AssignmentHub.Application.Common;

/// <summary>
/// Claim names used in our access tokens.
/// </summary>
/// <remarks>
/// Short JWT-standard names rather than the long WIF/SOAP claim URIs. Inbound
/// claim mapping is switched off in <c>Program.cs</c>, so the names written here
/// by the token generator are exactly the names read back from
/// <see cref="System.Security.Claims.ClaimsPrincipal"/> — what you sign is what
/// you get, with no hidden translation table in between.
/// </remarks>
public static class AppClaimTypes
{
    /// <summary>Subject: the user's <c>Guid</c> id.</summary>
    public const string UserId = "sub";

    public const string Email = "email";

    /// <summary>
    /// Matched against <c>[Authorize(Roles = ...)]</c> because
    /// <c>TokenValidationParameters.RoleClaimType</c> is set to this value.
    /// </summary>
    public const string Role = "role";

    public const string Name = "name";
}
