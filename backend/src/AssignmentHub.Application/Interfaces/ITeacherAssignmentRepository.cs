using AssignmentHub.Domain.Entities;

namespace AssignmentHub.Application.Interfaces;

/// <summary>
/// Read/write access to <see cref="TeacherAssignment"/>, the row that says a teacher is
/// entitled to teach one subject to one class.
/// </summary>
public interface ITeacherAssignmentRepository
{
    /// <summary>
    /// True when this teacher holds this exact class/subject pair. This is the
    /// only authority for "may this teacher set work here"; a Teacher role on its
    /// own answers a different and much weaker question.
    /// </summary>
    Task<bool> ExistsAsync(
        Guid teacherId,
        Guid classRoomId,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all class/subject pairs assigned to this teacher.
    /// </summary>
    Task<IReadOnlyList<TeacherAssignment>> ListForTeacherAsync(
        Guid teacherId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new entitlement. Returns false when the unique triple index is
    /// violated (duplicate mapping).
    /// </summary>
    Task<bool> TryAddAsync(TeacherAssignment entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all entitlements with resolved teacher, class, and subject names.
    /// Used by admin management endpoints.
    /// </summary>
    Task<IReadOnlyList<TeacherAssignment>> ListAllAsync(CancellationToken cancellationToken = default);
}
