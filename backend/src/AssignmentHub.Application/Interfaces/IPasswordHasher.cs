namespace AssignmentHub.Application.Interfaces;

/// <summary>
/// Password hashing, abstracted so Application depends on no concrete identity or
/// cryptography package and services stay unit-testable.
/// </summary>
/// <remarks>
/// One implementation, one hashing policy: the seeder and the login path both go
/// through this interface, so a hash written by one is always verifiable by the
/// other.
/// </remarks>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// True when <paramref name="password"/> matches <paramref name="passwordHash"/>.
    /// Never throws on malformed input — a bad hash is simply a failed match.
    /// </summary>
    bool Verify(string passwordHash, string password);
}
