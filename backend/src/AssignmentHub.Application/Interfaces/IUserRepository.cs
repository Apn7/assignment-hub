using AssignmentHub.Domain.Entities;

namespace AssignmentHub.Application.Interfaces;

/// <summary>
/// Read/write access to <see cref="User"/>. Deliberately narrow: it exposes the
/// queries services actually need rather than a generic repository, which keeps
/// service unit tests to a single mocked method.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Finds a user by their exact stored email, or null if there is none.
    /// Callers are expected to have normalised the address first.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
