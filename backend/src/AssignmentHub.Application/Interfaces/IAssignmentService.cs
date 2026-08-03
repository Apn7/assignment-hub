using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Assignments;

namespace AssignmentHub.Application.Interfaces;

/// <summary>
/// Every assignment business rule lives behind this interface, so all of them are
/// testable without HTTP and none of them can be bypassed by calling a different
/// endpoint.
/// </summary>
/// <remarks>
/// The acting user's id is a parameter rather than something the service reads
/// from an ambient context. That keeps the Application layer free of
/// <c>HttpContext</c> and makes "acting as teacher2 on teacher1's assignment" a
/// one-line test.
/// </remarks>
public interface IAssignmentService
{
    /// <summary>
    /// Creates a draft. Fails <see cref="ResultStatus.Forbidden"/> unless the
    /// teacher holds a teacher assignment for the requested class and subject.
    /// </summary>
    Task<Result<AssignmentResponse>> CreateAsync(
        Guid teacherId,
        CreateAssignmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an assignment the teacher owns. Anyone else's assignment, and a
    /// nonexistent one, both come back <see cref="ResultStatus.NotFound"/>.
    /// </summary>
    Task<Result<AssignmentResponse>> UpdateAsync(
        Guid teacherId,
        Guid assignmentId,
        UpdateAssignmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Moves a draft to published. Not idempotent: publishing twice conflicts.</summary>
    Task<Result<AssignmentResponse>> PublishAsync(
        Guid teacherId,
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a draft. A published assignment conflicts.</summary>
    Task<Result> DeleteAsync(
        Guid teacherId,
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    /// <summary>The teacher's own assignments, drafts included.</summary>
    Task<Result<IReadOnlyList<AssignmentResponse>>> ListForTeacherAsync(
        Guid teacherId,
        AssignmentFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Published assignments for the student's own class. The class is resolved
    /// from the stored user record, never from the request.
    /// </summary>
    Task<Result<IReadOnlyList<AssignmentResponse>>> ListForStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// One assignment, if this student is allowed to see it. Drafts and other
    /// classes' assignments are <see cref="ResultStatus.NotFound"/>, not
    /// forbidden, so an id cannot be used to discover what exists.
    /// </summary>
    Task<Result<AssignmentResponse>> GetForStudentAsync(
        Guid studentId,
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    /// <summary>Every assignment in the system. Admin only.</summary>
    Task<Result<IReadOnlyList<AssignmentResponse>>> ListAllAsync(
        AssignmentFilter filter,
        CancellationToken cancellationToken = default);
}
