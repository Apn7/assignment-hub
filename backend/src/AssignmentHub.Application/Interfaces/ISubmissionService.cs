using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Submissions;

namespace AssignmentHub.Application.Interfaces;

/// <summary>
/// Every submission business rule lives behind this interface: deadlines,
/// one-per-student, who may grade, and what a mark is allowed to be.
/// </summary>
/// <remarks>
/// As with <see cref="IAssignmentService"/>, the acting user's id is a parameter
/// rather than something read from an ambient context, so "student2 reading
/// student1's submission" is a one-line test.
/// </remarks>
public interface ISubmissionService
{
    /// <summary>
    /// Records a student's answer. The assignment must be published and belong to
    /// their class, or the result is <see cref="ResultStatus.NotFound"/>.
    /// </summary>
    Task<Result<SubmissionResponse>> SubmitAsync(
        Guid studentId,
        Guid assignmentId,
        SubmitAnswerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revises the student's own answer. Rejected once the deadline has passed or
    /// the work has been reviewed.
    /// </summary>
    Task<Result<SubmissionResponse>> UpdateOwnAsync(
        Guid studentId,
        Guid assignmentId,
        UpdateSubmissionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The student's own submission, including status, marks and feedback once
    /// those exist.
    /// </summary>
    Task<Result<SubmissionResponse>> GetOwnAsync(
        Guid studentId,
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every submission on one of the teacher's own assignments. Another teacher's
    /// assignment is <see cref="ResultStatus.NotFound"/>.
    /// </summary>
    Task<Result<IReadOnlyList<SubmissionListItem>>> ListForAssignmentAsync(
        Guid teacherId,
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    /// <summary>One submission in full, if it sits on the teacher's own assignment.</summary>
    Task<Result<SubmissionResponse>> GetForTeacherAsync(
        Guid teacherId,
        Guid submissionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records marks and feedback and moves the submission to
    /// <c>Reviewed</c>. Re-grading an already reviewed submission is allowed.
    /// </summary>
    Task<Result<SubmissionResponse>> GradeAsync(
        Guid teacherId,
        Guid submissionId,
        GradeSubmissionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the submission's status without touching marks or feedback. Used to
    /// reopen reviewed work so the student can revise it.
    /// </summary>
    Task<Result<SubmissionResponse>> ChangeStatusAsync(
        Guid teacherId,
        Guid submissionId,
        ChangeSubmissionStatusRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Every submission in the system. Admin only.</summary>
    Task<Result<IReadOnlyList<SubmissionListItem>>> ListAllAsync(
        SubmissionFilter filter,
        CancellationToken cancellationToken = default);
}
