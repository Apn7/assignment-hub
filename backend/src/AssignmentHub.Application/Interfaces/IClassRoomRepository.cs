using AssignmentHub.Domain.Entities;

namespace AssignmentHub.Application.Interfaces;

/// <summary>
/// Read/write access to <see cref="ClassRoom"/>.
/// </summary>
public interface IClassRoomRepository
{
    Task<ClassRoom?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive name match for duplicate detection.</summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    Task AddAsync(ClassRoom classRoom, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassRoom>> ListAsync(CancellationToken cancellationToken = default);
}
