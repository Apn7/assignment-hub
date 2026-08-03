using AssignmentHub.Application.DTOs.Submissions;
using AssignmentHub.Domain.Entities;

namespace AssignmentHub.Application.Interfaces;

/// <summary>
/// Read/write access to the <see cref="Submission"/> aggregate. As with
/// assignments, the method names say what each query is <em>for</em>, because the
/// tracking behaviour and included navigations differ by purpose.
/// </summary>
public interface ISubmissionRepository
{
    /// <summary>
    /// Inserts a submission, or reports that this student already has one.
    /// </summary>
    /// <returns>
    /// <c>true</c> when the row was created; <c>false</c> when the unique index on
    /// (assignment, student) rejected it.
    /// </returns>
    /// <remarks>
    /// The service checks for an existing submission first, so a <c>false</c> here
    /// means two of the student's requests raced. Surfacing that as a value rather
    /// than letting a driver exception escape is what turns the race into a 409
    /// instead of a 500 — and the check stays in the database, which is the only
    /// place that can win a race.
    /// </remarks>
    Task<bool> TryAddAsync(Submission submission, CancellationToken cancellationToken = default);

    /// <summary>True when this student already has a submission on this assignment.</summary>
    Task<bool> ExistsForAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a submission for mutation, with its parent assignment. Tracked.
    /// </summary>
    /// <remarks>
    /// The assignment comes along because every write rule needs it: the deadline
    /// for a student edit, <c>MaxMarks</c> for grading, and
    /// <c>CreatedByTeacherId</c> for the ownership check.
    /// </remarks>
    Task<Submission?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a student's own submission on one assignment for mutation, with its
    /// parent assignment. Tracked. The student id is part of the query, so
    /// ownership is not something the caller can forget to check.
    /// </summary>
    Task<Submission?> GetOwnForUpdateAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a submission with everything a response needs. Untracked, so it
    /// reflects what was actually persisted.
    /// </summary>
    Task<Submission?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>A student's own submission on one assignment, for a response.</summary>
    Task<Submission?> GetOwnDetailAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>Every submission on one assignment, earliest first.</summary>
    Task<IReadOnlyList<Submission>> ListForAssignmentAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    /// <summary>Every submission in the system. Admin only. Most recent first.</summary>
    Task<IReadOnlyList<Submission>> ListAllAsync(
        SubmissionFilter filter,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
