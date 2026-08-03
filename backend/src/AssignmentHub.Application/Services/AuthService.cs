using AssignmentHub.Application.DTOs.Auth;
using AssignmentHub.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AssignmentHub.Application.Services;

/// <inheritdoc cref="IAuthService"/>
public sealed class AuthService : IAuthService
{
    /// <summary>
    /// A well-formed hash of a value nobody knows, verified against when the email
    /// is unknown so both failure paths do the same PBKDF2 work. Computed once per
    /// process on first use — a hardcoded constant would be cheaper but reads like
    /// an unexplained secret, and hashing per request would be wasteful.
    /// </summary>
    private static string? _decoyPasswordHash;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        ILogger<AuthService> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormaliseEmail(request.Email);

        var user = await _users.GetByEmailAsync(email, cancellationToken);

        // Always verify something. When the address is unknown we verify against the
        // decoy, so an unknown email and a wrong password take the same time as well
        // as returning the same result.
        var hashToVerify = user?.PasswordHash ?? DecoyPasswordHash();
        var passwordMatches = _passwordHasher.Verify(hashToVerify, request.Password);

        if (user is null || !passwordMatches)
        {
            _logger.LogWarning("Failed login attempt for {Email}.", email);
            return null;
        }

        var token = _tokenGenerator.Generate(user);

        _logger.LogInformation("User {UserId} signed in as {Role}.", user.Id, user.Role);

        return new LoginResponse
        {
            AccessToken = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = UserSummary.FromUser(user)
        };
    }

    /// <summary>
    /// Emails are stored lower-cased, so normalising the input here lets people log
    /// in regardless of how their mail client capitalised the address.
    /// </summary>
    private static string NormaliseEmail(string email) => email.Trim().ToLowerInvariant();

    private string DecoyPasswordHash() =>
        _decoyPasswordHash ??= _passwordHasher.Hash(Guid.NewGuid().ToString("N"));
}
