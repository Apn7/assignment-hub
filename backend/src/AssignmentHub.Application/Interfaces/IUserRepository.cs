using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;

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

    /// <summary>
    /// Persists a new user. Returns false when the unique email index is violated
    /// (same two-layer conflict pattern as <c>ISubmissionRepository.TryAddAsync</c>).
    /// </summary>
    Task<bool> TryAddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists users, optionally filtered by role. Includes the ClassRoom
    /// navigation for students.
    /// </summary>
    Task<IReadOnlyList<User>> ListAsync(UserRole? roleFilter = null, CancellationToken cancellationToken = default);
}
