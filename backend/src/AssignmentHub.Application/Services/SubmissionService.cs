using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Submissions;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AssignmentHub.Application.Services;

/// <inheritdoc cref="ISubmissionService"/>
public sealed class SubmissionService : ISubmissionService
{
    private const string DuplicateMessage =
        "You have already submitted an answer to this assignment. Update it instead.";

    private const string DeadlinePassedMessage =
        "The deadline for this assignment has passed.";

    private const string AlreadyReviewedMessage =
        "This submission has been reviewed and can no longer be changed. Ask your teacher to reopen it.";

    private readonly ISubmissionRepository _submissions;
    private readonly IAssignmentRepository _assignments;
    private readonly IUserRepository _users;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        ISubmissionRepository submissions,
        IAssignmentRepository assignments,
        IUserRepository users,
        TimeProvider timeProvider,
        ILogger<SubmissionService> logger)
    {
        _submissions = submissions;
        _assignments = assignments;
        _users = users;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // -----------------------------------------------------------------------
    // Student
    // -----------------------------------------------------------------------

    public async Task<Result<SubmissionResponse>> SubmitAsync(
        Guid studentId,
        Guid assignmentId,
        SubmitAnswerRequest request,
        CancellationToken cancellationToken = default)
    {
        var student = await _users.GetByIdAsync(studentId, cancellationToken);

        if (student?.ClassRoomId is not { } classRoomId)
        {
            _logger.LogWarning(
                "Student {StudentId} belongs to no class, so no assignment is submittable.", studentId);

            return Result<SubmissionResponse>.NotFound(NotFoundMessages.Assignment);
        }

        // Rule 1. The same visibility predicate the student listing uses, so a draft
        // or another class's assignment is not merely refused — it is never loaded.
        var assignment = await _assignments.GetVisibleToStudentAsync(
            assignmentId, classRoomId, cancellationToken);

        if (assignment is null)
        {
            _logger.LogInformation(
                "Student {StudentId} tried to submit to assignment {AssignmentId}, which is not visible to their class.",
                studentId,
                assignmentId);

            return Result<SubmissionResponse>.NotFound(NotFoundMessages.Assignment);
        }

        // Rule 2, checked before the duplicate check so a student who missed the
        // deadline entirely is told about the deadline rather than about their
        // (nonexistent) earlier attempt.
        if (IsPastDeadline(assignment))
        {
            return Result<SubmissionResponse>.Conflict(DeadlinePassedMessage);
        }

        // Rule 3, first pass: the friendly answer for the ordinary case.
        if (await _submissions.ExistsForAsync(assignmentId, studentId, cancellationToken))
        {
            return Result<SubmissionResponse>.Conflict(DuplicateMessage);
        }

        var now = UtcNow();

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignmentId,
            StudentId = studentId,
            AnswerText = request.AnswerText.Trim(),
            SubmittedAt = now,
            // Equal to SubmittedAt until the student revises it.
            UpdatedAt = now,
            Status = SubmissionStatus.Submitted
        };

        // Rule 3, second pass. Between the check above and this insert, another
        // request from the same student can slip through; only the unique index can
        // settle it, so a rejection there becomes the same 409 rather than a 500.
        if (!await _submissions.TryAddAsync(submission, cancellationToken))
        {
            _logger.LogWarning(
                "Concurrent submissions from student {StudentId} for assignment {AssignmentId}; the unique index rejected the second.",
                studentId,
                assignmentId);

            return Result<SubmissionResponse>.Conflict(DuplicateMessage);
        }

        _logger.LogInformation(
            "Student {StudentId} submitted {SubmissionId} for assignment {AssignmentId}.",
            studentId,
            submission.Id,
            assignmentId);

        return await ReadBackAsync(submission.Id, cancellationToken);
    }

    public async Task<Result<SubmissionResponse>> UpdateOwnAsync(
        Guid studentId,
        Guid assignmentId,
        UpdateSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Ownership is part of the query, and the parent assignment comes with it so
        // the deadline is available without a second round trip.
        var submission = await _submissions.GetOwnForUpdateAsync(
            assignmentId, studentId, cancellationToken);

        if (submission is null)
        {
            return Result<SubmissionResponse>.NotFound(NotFoundMessages.Submission);
        }

        // Rule 4 before rule 2. Both may be true at once, and "it has been graded,
        // ask for it to be reopened" is the one the student can act on; the deadline
        // message would be a dead end.
        if (submission.Status == SubmissionStatus.Reviewed)
        {
            return Result<SubmissionResponse>.Conflict(AlreadyReviewedMessage);
        }

        // Rule 2.
        if (IsPastDeadline(submission.Assignment))
        {
            return Result<SubmissionResponse>.Conflict(DeadlinePassedMessage);
        }

        submission.AnswerText = request.AnswerText.Trim();
        submission.UpdatedAt = UtcNow();

        await _submissions.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Student {StudentId} revised submission {SubmissionId}.", studentId, submission.Id);

        return await ReadBackAsync(submission.Id, cancellationToken);
    }

    public async Task<Result<SubmissionResponse>> GetOwnAsync(
        Guid studentId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var submission = await _submissions.GetOwnDetailAsync(
            assignmentId, studentId, cancellationToken);

        return submission is null
            ? Result<SubmissionResponse>.NotFound(NotFoundMessages.Submission)
            : Result<SubmissionResponse>.Success(SubmissionResponse.FromSubmission(submission));
    }

    // -----------------------------------------------------------------------
    // Teacher
    // -----------------------------------------------------------------------

    public async Task<Result<IReadOnlyList<SubmissionListItem>>> ListForAssignmentAsync(
        Guid teacherId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _assignments.GetDetailAsync(assignmentId, cancellationToken);

        // Rule 5. The route names an assignment, so the answer for "not yours" is the
        // same one AssignmentService gives for the same id.
        if (assignment is null || assignment.CreatedByTeacherId != teacherId)
        {
            LogDeniedAssignmentAccess(teacherId, assignmentId, assignment);
            return Result<IReadOnlyList<SubmissionListItem>>.NotFound(NotFoundMessages.Assignment);
        }

        var submissions = await _submissions.ListForAssignmentAsync(assignmentId, cancellationToken);

        return Result<IReadOnlyList<SubmissionListItem>>.Success(Project(submissions));
    }

    public async Task<Result<SubmissionResponse>> GetForTeacherAsync(
        Guid teacherId,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        var submission = await _submissions.GetDetailAsync(submissionId, cancellationToken);

        // Rule 5.
        if (!BelongsToTeacher(submission, teacherId))
        {
            LogDeniedSubmissionAccess(teacherId, submissionId, submission, "read");
            return Result<SubmissionResponse>.NotFound(NotFoundMessages.Submission);
        }

        return Result<SubmissionResponse>.Success(SubmissionResponse.FromSubmission(submission!));
    }

    public async Task<Result<SubmissionResponse>> GradeAsync(
        Guid teacherId,
        Guid submissionId,
        GradeSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var submission = await _submissions.GetForUpdateAsync(submissionId, cancellationToken);

        // Rule 5.
        if (!BelongsToTeacher(submission, teacherId))
        {
            LogDeniedSubmissionAccess(teacherId, submissionId, submission, "grade");
            return Result<SubmissionResponse>.NotFound(NotFoundMessages.Submission);
        }

        // Rule 6. The upper bound belongs to the assignment, which is why this cannot
        // be a request-validation rule and why the message names the actual maximum.
        var maxMarks = submission!.Assignment.MaxMarks;

        if (request.Marks < SubmissionRules.MinMarks || request.Marks > maxMarks)
        {
            return Result<SubmissionResponse>.Unprocessable(
                $"Marks must be between {SubmissionRules.MinMarks} and {maxMarks} for this assignment.");
        }

        var regrade = submission.Status == SubmissionStatus.Reviewed;

        submission.Marks = request.Marks;
        // An omitted feedback field clears any previous comment rather than silently
        // keeping it: a re-grade sends the whole verdict, not a patch of it.
        submission.Feedback = string.IsNullOrWhiteSpace(request.Feedback)
            ? null
            : request.Feedback.Trim();
        submission.Status = SubmissionStatus.Reviewed;
        submission.ReviewedAt = UtcNow();

        await _submissions.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teacher {TeacherId} {Action} submission {SubmissionId} at {Marks}/{MaxMarks}.",
            teacherId,
            regrade ? "re-graded" : "graded",
            submissionId,
            request.Marks,
            maxMarks);

        return await ReadBackAsync(submissionId, cancellationToken);
    }

    public async Task<Result<SubmissionResponse>> ChangeStatusAsync(
        Guid teacherId,
        Guid submissionId,
        ChangeSubmissionStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var submission = await _submissions.GetForUpdateAsync(submissionId, cancellationToken);

        // Rule 5.
        if (!BelongsToTeacher(submission, teacherId))
        {
            LogDeniedSubmissionAccess(teacherId, submissionId, submission, "restatus");
            return Result<SubmissionResponse>.NotFound(NotFoundMessages.Submission);
        }

        var previous = submission!.Status;

        // Only the status moves. Marks, feedback and ReviewedAt are left exactly as
        // they were: reopening work for revision is not the same as withdrawing the
        // mark, and a teacher who wants to change the mark grades it again. Setting
        // the status it already has is a no-op rather than a conflict — this endpoint
        // sets state, unlike publish, which is an event.
        submission.Status = request.Status;

        await _submissions.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teacher {TeacherId} moved submission {SubmissionId} from {Previous} to {Status}; marks left at {Marks}.",
            teacherId,
            submissionId,
            previous,
            request.Status,
            submission.Marks);

        return await ReadBackAsync(submissionId, cancellationToken);
    }

    // -----------------------------------------------------------------------
    // Admin
    // -----------------------------------------------------------------------

    public async Task<Result<IReadOnlyList<SubmissionListItem>>> ListAllAsync(
        SubmissionFilter filter,
        CancellationToken cancellationToken = default)
    {
        var submissions = await _submissions.ListAllAsync(filter, cancellationToken);

        return Result<IReadOnlyList<SubmissionListItem>>.Success(Project(submissions));
    }

    // -----------------------------------------------------------------------
    // Internals
    // -----------------------------------------------------------------------

    /// <summary>
    /// Rule 2's comparison, in one place so submit and update cannot disagree.
    /// </summary>
    /// <remarks>
    /// Strictly greater-than: a submission landing on the deadline instant is on
    /// time. Note this is the opposite convention from publishing an assignment,
    /// which requires a deadline strictly in the future — deliberately so. There,
    /// the point is that students need time to work; here, the point is that a
    /// student who makes the deadline exactly has made it.
    /// </remarks>
    private bool IsPastDeadline(Assignment? assignment) =>
        assignment is not null && UtcNow() > assignment.Deadline;

    private static bool BelongsToTeacher(Submission? submission, Guid teacherId) =>
        submission?.Assignment is not null && submission.Assignment.CreatedByTeacherId == teacherId;

    private static IReadOnlyList<SubmissionListItem> Project(IReadOnlyList<Submission> submissions) =>
        submissions.Select(SubmissionListItem.FromSubmission).ToList();

    /// <summary>
    /// Re-reads the submission after a write, so the response carries the student
    /// and assignment names and reflects what the database now holds.
    /// </summary>
    private async Task<Result<SubmissionResponse>> ReadBackAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        var submission = await _submissions.GetDetailAsync(submissionId, cancellationToken)
                         ?? throw new InvalidOperationException(
                             $"Submission {submissionId} could not be read back immediately after being saved.");

        return Result<SubmissionResponse>.Success(SubmissionResponse.FromSubmission(submission));
    }

    private void LogDeniedAssignmentAccess(Guid teacherId, Guid assignmentId, Assignment? assignment)
    {
        if (assignment is null)
        {
            _logger.LogInformation(
                "Teacher {TeacherId} asked for submissions on assignment {AssignmentId}, which does not exist.",
                teacherId,
                assignmentId);
            return;
        }

        _logger.LogWarning(
            "Teacher {TeacherId} asked for submissions on assignment {AssignmentId}, which belongs to teacher {OwnerId}.",
            teacherId,
            assignmentId,
            assignment.CreatedByTeacherId);
    }

    private void LogDeniedSubmissionAccess(
        Guid teacherId,
        Guid submissionId,
        Submission? submission,
        string action)
    {
        // Split so the server can tell the cases apart even though the response
        // cannot. "Someone else's" is the one worth noticing.
        if (submission?.Assignment is null)
        {
            _logger.LogInformation(
                "Teacher {TeacherId} tried to {Action} submission {SubmissionId}, which does not exist.",
                teacherId,
                action,
                submissionId);
            return;
        }

        _logger.LogWarning(
            "Teacher {TeacherId} tried to {Action} submission {SubmissionId}, which sits on teacher {OwnerId}'s assignment.",
            teacherId,
            action,
            submissionId,
            submission.Assignment.CreatedByTeacherId);
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
}
