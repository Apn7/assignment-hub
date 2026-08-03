using AssignmentHub.Application.DTOs.Assignments;
using AssignmentHub.Domain.Entities;

namespace AssignmentHub.Application.Interfaces;

/// <summary>
/// Read/write access to the <see cref="Assignment"/> aggregate. The method names
/// say what each query is <em>for</em>, because the tracking behaviour and the
/// included navigations differ by purpose and getting them wrong is silent.
/// </summary>
public interface IAssignmentRepository
{
    /// <summary>
    /// Loads an assignment for mutation. Tracked, no navigations: the caller
    /// checks ownership and status, then writes fields.
    /// </summary>
    Task<Assignment?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads an assignment with its class, subject and teacher, for building a
    /// response. Untracked, so it always reflects what was actually persisted.
    /// </summary>
    Task<Assignment?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads an assignment only if a student of <paramref name="classRoomId"/> is
    /// allowed to see it, per <c>AssignmentQueries.VisibleToStudent</c>. Returns
    /// null for a draft, another class's assignment, and a nonexistent id alike —
    /// the caller cannot tell which, and must not be able to.
    /// </summary>
    Task<Assignment?> GetVisibleToStudentAsync(
        Guid id,
        Guid classRoomId,
        CancellationToken cancellationToken = default);

    /// <summary>Assignments created by this teacher, both statuses. Newest first.</summary>
    Task<IReadOnlyList<Assignment>> ListForTeacherAsync(
        Guid teacherId,
        AssignmentFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>Published assignments for one class, nearest deadline first.</summary>
    Task<IReadOnlyList<Assignment>> ListVisibleToStudentAsync(
        Guid classRoomId,
        CancellationToken cancellationToken = default);

    /// <summary>Every assignment in the system. Admin only. Newest first.</summary>
    Task<IReadOnlyList<Assignment>> ListAllAsync(
        AssignmentFilter filter,
        CancellationToken cancellationToken = default);

    void Add(Assignment assignment);

    void Remove(Assignment assignment);

    /// <summary>
    /// Commits pending changes. Exposed here rather than as a separate unit of
    /// work because a service method only ever touches this one aggregate.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
