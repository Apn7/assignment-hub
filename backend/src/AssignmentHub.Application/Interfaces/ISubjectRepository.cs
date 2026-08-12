using AssignmentHub.Domain.Entities;

namespace AssignmentHub.Application.Interfaces;

/// <summary>
/// Read/write access to <see cref="Subject"/>.
/// </summary>
public interface ISubjectRepository
{
    Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive name match for duplicate detection.</summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    Task AddAsync(Subject subject, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subject>> ListAsync(CancellationToken cancellationToken = default);
}
