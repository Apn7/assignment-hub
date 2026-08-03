using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AssignmentHub.Infrastructure.Services;

/// <summary>
/// <see cref="IPasswordHasher"/> backed by ASP.NET Core Identity's
/// <see cref="PasswordHasher{TUser}"/> (PBKDF2-HMAC-SHA256, salted, with a
/// versioned hash format).
/// </summary>
/// <remarks>
/// Only Identity's hasher is used — no Identity stores, schema, middleware or UI.
/// Wrapping it keeps that dependency inside Infrastructure and gives Application a
/// two-method surface to mock.
/// </remarks>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly IPasswordHasher<User> _inner;

    public IdentityPasswordHasher(IPasswordHasher<User> inner)
    {
        _inner = inner;
    }

    public string Hash(string password) => _inner.HashPassword(EmptyUser, password);

    public bool Verify(string passwordHash, string password)
    {
        if (string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        var result = _inner.VerifyHashedPassword(EmptyUser, passwordHash, password);

        // SuccessRehashNeeded means the password is correct but was hashed with older
        // parameters. Treated as success; transparent rehashing would belong on a
        // password-change path, not here.
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }

    /// <summary>
    /// Identity's hasher takes a user argument it never reads for the default
    /// (non-rehashing) configuration, so a throwaway instance keeps our interface
    /// free of a parameter that carries no meaning.
    /// </summary>
    private static User EmptyUser { get; } = new();
}
