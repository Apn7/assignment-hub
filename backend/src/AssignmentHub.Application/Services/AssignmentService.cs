using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Assignments;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AssignmentHub.Application.Services;

/// <inheritdoc cref="IAssignmentService"/>
public sealed class AssignmentService : IAssignmentService
{
    /// <summary>
    /// One message for every not-found outcome. A missing assignment, a
    /// colleague's assignment, a draft a student probed for and another class's
    /// assignment all produce this exact string, so a caller holding an id learns
    /// nothing about whether it exists. Shared with <c>SubmissionService</c>, which
    /// answers for the same resource on its own routes.
    /// </summary>
    private const string NotFoundMessage = NotFoundMessages.Assignment;

    /// <summary>
    /// Used only where the caller supplied the class and subject themselves, so
    /// naming them back leaks nothing they did not already know.
    /// </summary>
    private const string NotAssignedMessage =
        "You are not assigned to teach this subject to this class.";

    private readonly IAssignmentRepository _assignments;
    private readonly ITeacherAssignmentRepository _teacherAssignments;
    private readonly IUserRepository _users;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AssignmentService> _logger;

    public AssignmentService(
        IAssignmentRepository assignments,
        ITeacherAssignmentRepository teacherAssignments,
        IUserRepository users,
        TimeProvider timeProvider,
        ILogger<AssignmentService> logger)
    {
        _assignments = assignments;
        _teacherAssignments = teacherAssignments;
        _users = users;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<AssignmentResponse>> CreateAsync(
        Guid teacherId,
        CreateAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        // Rule 3. Also enforced by the validator at the edge; repeated here so the
        // rule holds for any caller of the service, not just HTTP ones.
        if (ValidateMaxMarks(request.MaxMarks) is { } marksError)
        {
            return Result<AssignmentResponse>.Invalid(marksError);
        }

        // Rule 1.
        if (!await _teacherAssignments.ExistsAsync(
                teacherId, request.ClassRoomId, request.SubjectId, cancellationToken))
        {
            _logger.LogWarning(
                "Teacher {TeacherId} tried to create an assignment for class {ClassRoomId} / subject {SubjectId}, which they are not assigned to.",
                teacherId,
                request.ClassRoomId,
                request.SubjectId);

            return Result<AssignmentResponse>.Forbidden(NotAssignedMessage);
        }

        var now = UtcNow();

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ClassRoomId = request.ClassRoomId,
            SubjectId = request.SubjectId,
            CreatedByTeacherId = teacherId,
            Deadline = ToUtc(request.Deadline),
            MaxMarks = request.MaxMarks,
            // Never published on creation. Making work visible to students is an
            // explicit second action, so it cannot happen by accident.
            Status = AssignmentStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        _assignments.Add(assignment);
        await _assignments.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teacher {TeacherId} created draft assignment {AssignmentId}.", teacherId, assignment.Id);

        return await ReadBackAsync(assignment.Id, cancellationToken);
    }

    public async Task<Result<AssignmentResponse>> UpdateAsync(
        Guid teacherId,
        Guid assignmentId,
        UpdateAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (ValidateMaxMarks(request.MaxMarks) is { } marksError)
        {
            return Result<AssignmentResponse>.Invalid(marksError);
        }

        var assignment = await _assignments.GetForUpdateAsync(assignmentId, cancellationToken);

        // Rule 2.
        if (assignment is null || assignment.CreatedByTeacherId != teacherId)
        {
            LogDeniedAccess(teacherId, assignmentId, assignment, "update");
            return Result<AssignmentResponse>.NotFound(NotFoundMessage);
        }

        var deadline = ToUtc(request.Deadline);
        var targetMoved = request.ClassRoomId != assignment.ClassRoomId
                          || request.SubjectId != assignment.SubjectId;

        if (assignment.Status == AssignmentStatus.Published)
        {
            // Rule 5. Students have already seen this, and marks and submissions
            // will be interpreted against it, so the parts that change its meaning
            // are frozen. Only the wording and a later deadline stay editable.
            if (targetMoved)
            {
                return Result<AssignmentResponse>.Conflict(
                    "A published assignment cannot be moved to a different class or subject.");
            }

            if (request.MaxMarks != assignment.MaxMarks)
            {
                return Result<AssignmentResponse>.Conflict(
                    "The maximum marks of a published assignment cannot be changed.");
            }

            if (deadline < assignment.Deadline)
            {
                return Result<AssignmentResponse>.Conflict(
                    "The deadline of a published assignment can be extended but not brought forward.");
            }
        }
        else if (targetMoved)
        {
            // Rule 1 again, and the reason it is checked twice: creating a draft for
            // a class you do teach and then re-pointing it at one you do not would
            // otherwise be a way straight through the create-time check.
            if (!await _teacherAssignments.ExistsAsync(
                    teacherId, request.ClassRoomId, request.SubjectId, cancellationToken))
            {
                _logger.LogWarning(
                    "Teacher {TeacherId} tried to move draft assignment {AssignmentId} to class {ClassRoomId} / subject {SubjectId}, which they are not assigned to.",
                    teacherId,
                    assignmentId,
                    request.ClassRoomId,
                    request.SubjectId);

                return Result<AssignmentResponse>.Forbidden(NotAssignedMessage);
            }
        }

        assignment.Title = request.Title.Trim();
        assignment.Description = request.Description.Trim();
        assignment.ClassRoomId = request.ClassRoomId;
        assignment.SubjectId = request.SubjectId;
        assignment.Deadline = deadline;
        assignment.MaxMarks = request.MaxMarks;
        assignment.UpdatedAt = UtcNow();

        // Status is deliberately absent from the assignments above. Together with
        // the update request having no status field, that is rule 6: this method
        // cannot move an assignment in either direction along the status axis.

        await _assignments.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teacher {TeacherId} updated assignment {AssignmentId}.", teacherId, assignmentId);

        return await ReadBackAsync(assignmentId, cancellationToken);
    }

    public async Task<Result<AssignmentResponse>> PublishAsync(
        Guid teacherId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _assignments.GetForUpdateAsync(assignmentId, cancellationToken);

        // Rule 2.
        if (assignment is null || assignment.CreatedByTeacherId != teacherId)
        {
            LogDeniedAccess(teacherId, assignmentId, assignment, "publish");
            return Result<AssignmentResponse>.NotFound(NotFoundMessage);
        }

        // Publishing is a one-time event, not a state to converge on: it is what
        // makes work visible to a class, and later it is where a notification would
        // fire. A silent second success would hide a double-submitting client.
        if (assignment.Status == AssignmentStatus.Published)
        {
            return Result<AssignmentResponse>.Conflict("This assignment is already published.");
        }

        // Rule 4. Publishing work that is already overdue gives students a deadline
        // they cannot meet, so the deadline must be strictly in the future.
        if (assignment.Deadline <= UtcNow())
        {
            return Result<AssignmentResponse>.Conflict(
                "This assignment cannot be published because its deadline is not in the future. Extend the deadline first.");
        }

        assignment.Status = AssignmentStatus.Published;
        assignment.UpdatedAt = UtcNow();

        await _assignments.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teacher {TeacherId} published assignment {AssignmentId}.", teacherId, assignmentId);

        return await ReadBackAsync(assignmentId, cancellationToken);
    }

    public async Task<Result> DeleteAsync(
        Guid teacherId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _assignments.GetForUpdateAsync(assignmentId, cancellationToken);

        // Rule 2.
        if (assignment is null || assignment.CreatedByTeacherId != teacherId)
        {
            LogDeniedAccess(teacherId, assignmentId, assignment, "delete");
            return Result.NotFound(NotFoundMessage);
        }

        // Rule 7. A published assignment is something a class has already been
        // shown, and once submissions exist they hang off it. Withdrawing it is a
        // different operation from deleting it, and is not in scope.
        if (assignment.Status != AssignmentStatus.Draft)
        {
            return Result.Conflict(
                "A published assignment cannot be deleted. Students may already have seen it, and submissions are attached to it.");
        }

        _assignments.Remove(assignment);
        await _assignments.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teacher {TeacherId} deleted draft assignment {AssignmentId}.", teacherId, assignmentId);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<AssignmentResponse>>> ListForTeacherAsync(
        Guid teacherId,
        AssignmentFilter filter,
        CancellationToken cancellationToken = default)
    {
        var assignments = await _assignments.ListForTeacherAsync(teacherId, filter, cancellationToken);

        return Result<IReadOnlyList<AssignmentResponse>>.Success(Project(assignments));
    }

    public async Task<Result<IReadOnlyList<AssignmentResponse>>> ListForStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        // The class comes from the stored user record, never from the caller. It is
        // also not in the token: class membership is authorization data that would
        // then go stale until the student logged in again.
        var student = await _users.GetByIdAsync(studentId, cancellationToken);

        if (student?.ClassRoomId is not { } classRoomId)
        {
            // Nothing is being hidden and nothing the caller sends can fix it, so an
            // empty list is a more honest answer than an error.
            _logger.LogWarning(
                "Student {StudentId} belongs to no class, so no assignments are visible.", studentId);

            return Result<IReadOnlyList<AssignmentResponse>>.Success(Array.Empty<AssignmentResponse>());
        }

        // Rules 1, 2 and 8: the filtering is done by the database through
        // AssignmentQueries.VisibleToStudent, not by trimming a wider result here.
        var assignments = await _assignments.ListVisibleToStudentAsync(classRoomId, cancellationToken);

        return Result<IReadOnlyList<AssignmentResponse>>.Success(Project(assignments));
    }

    public async Task<Result<AssignmentResponse>> GetForStudentAsync(
        Guid studentId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var student = await _users.GetByIdAsync(studentId, cancellationToken);

        if (student?.ClassRoomId is not { } classRoomId)
        {
            return Result<AssignmentResponse>.NotFound(NotFoundMessage);
        }

        var assignment = await _assignments.GetVisibleToStudentAsync(
            assignmentId, classRoomId, cancellationToken);

        // Rule 8. A draft, another class's assignment and an id that never existed
        // are one outcome with one message.
        if (assignment is null)
        {
            _logger.LogInformation(
                "Student {StudentId} requested assignment {AssignmentId}, which is not visible to their class.",
                studentId,
                assignmentId);

            return Result<AssignmentResponse>.NotFound(NotFoundMessage);
        }

        return Result<AssignmentResponse>.Success(AssignmentResponse.FromAssignment(assignment));
    }

    public async Task<Result<IReadOnlyList<AssignmentResponse>>> ListAllAsync(
        AssignmentFilter filter,
        CancellationToken cancellationToken = default)
    {
        var assignments = await _assignments.ListAllAsync(filter, cancellationToken);

        return Result<IReadOnlyList<AssignmentResponse>>.Success(Project(assignments));
    }

    /// <summary>Rule 3, as a message or null if the value is acceptable.</summary>
    private static string? ValidateMaxMarks(int maxMarks) => maxMarks switch
    {
        < AssignmentRules.MinMaxMarks => "Maximum marks must be greater than zero.",
        > AssignmentRules.MaxAllowedMarks =>
            $"Maximum marks must not exceed {AssignmentRules.MaxAllowedMarks}.",
        _ => null
    };

    private static IReadOnlyList<AssignmentResponse> Project(IReadOnlyList<Assignment> assignments) =>
        assignments.Select(AssignmentResponse.FromAssignment).ToList();

    /// <summary>
    /// Re-reads the assignment after a write so the response carries the class,
    /// subject and teacher names, and reflects exactly what the database now holds
    /// rather than what this method believes it wrote.
    /// </summary>
    private async Task<Result<AssignmentResponse>> ReadBackAsync(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var assignment = await _assignments.GetDetailAsync(assignmentId, cancellationToken)
                         ?? throw new InvalidOperationException(
                             $"Assignment {assignmentId} could not be read back immediately after being saved.");

        return Result<AssignmentResponse>.Success(AssignmentResponse.FromAssignment(assignment));
    }

    private void LogDeniedAccess(Guid teacherId, Guid assignmentId, Assignment? assignment, string action)
    {
        // Logged apart so the server can tell the two cases apart even though the
        // response cannot. "Not yours" is the one worth noticing.
        if (assignment is null)
        {
            _logger.LogInformation(
                "Teacher {TeacherId} tried to {Action} assignment {AssignmentId}, which does not exist.",
                teacherId,
                action,
                assignmentId);
            return;
        }

        _logger.LogWarning(
            "Teacher {TeacherId} tried to {Action} assignment {AssignmentId}, which belongs to teacher {OwnerId}.",
            teacherId,
            action,
            assignmentId,
            assignment.CreatedByTeacherId);
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Normalises an inbound deadline to UTC so it can be compared with the clock.
    /// A JSON value ending in <c>Z</c> arrives as UTC; one without a timezone
    /// arrives as <see cref="DateTimeKind.Unspecified"/> and is read as UTC, which
    /// is the same reading the persistence layer's converter applies.
    /// </summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
