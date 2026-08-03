namespace AssignmentHub.Api.Configuration;

/// <summary>
/// Strongly typed view of the "Jwt" configuration section.
/// </summary>
/// <remarks>
/// Every value is supplied by appsettings, user-secrets or environment
/// variables (<c>Jwt__Secret</c>, <c>Jwt__Issuer</c>, ...). <see cref="Secret"/>
/// is intentionally absent from every committed file.
/// </remarks>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// HMAC-SHA256 signing key. Must be at least 32 characters (256 bits) or
    /// the token handler rejects it at runtime.
    /// </summary>
    public string Secret { get; init; } = string.Empty;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    /// <summary>Access token lifetime in minutes.</summary>
    public int AccessTokenMinutes { get; init; } = 60;
}
