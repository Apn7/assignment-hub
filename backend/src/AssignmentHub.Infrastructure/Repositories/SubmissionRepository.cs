using AssignmentHub.Application.DTOs.Submissions;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AssignmentHub.Infrastructure.Repositories;

/// <inheritdoc cref="ISubmissionRepository"/>
public sealed class SubmissionRepository : ISubmissionRepository
{
    /// <summary>
    /// PostgreSQL's SQLSTATE for unique_violation. The only unique index on
    /// Submissions is (AssignmentId, StudentId), so a 23505 raised while inserting a
    /// submission can only mean this student already has one.
    /// </summary>
    private const string UniqueViolation = "23505";

    private readonly AppDbContext _context;

    public SubmissionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(
        Submission submission,
        CancellationToken cancellationToken = default)
    {
        _context.Submissions.Add(submission);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // Detach the rejected entity, or it stays in Added state and the next
            // SaveChanges on this scoped context would try to insert it again.
            _context.Entry(submission).State = EntityState.Detached;
            return false;
        }
    }

    public Task<bool> ExistsForAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        // Matches the unique index, so this is an index probe rather than a scan.
        return _context.Submissions.AnyAsync(
            submission => submission.AssignmentId == assignmentId
                          && submission.StudentId == studentId,
            cancellationToken);
    }

    public Task<Submission?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Tracked, with the parent assignment: every write rule needs the deadline,
        // MaxMarks or the owning teacher.
        return _context.Submissions
            .Include(submission => submission.Assignment)
            .SingleOrDefaultAsync(submission => submission.Id == id, cancellationToken);
    }

    public Task<Submission?> GetOwnForUpdateAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return _context.Submissions
            .Include(submission => submission.Assignment)
            .SingleOrDefaultAsync(
                submission => submission.AssignmentId == assignmentId
                              && submission.StudentId == studentId,
                cancellationToken);
    }

    public Task<Submission?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return WithRelatedNames(_context.Submissions.AsNoTracking())
            .SingleOrDefaultAsync(submission => submission.Id == id, cancellationToken);
    }

    public Task<Submission?> GetOwnDetailAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return WithRelatedNames(_context.Submissions.AsNoTracking())
            .SingleOrDefaultAsync(
                submission => submission.AssignmentId == assignmentId
                              && submission.StudentId == studentId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Submission>> ListForAssignmentAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        return await WithRelatedNames(_context.Submissions.AsNoTracking())
            .Where(submission => submission.AssignmentId == assignmentId)
            // Earliest first: a teacher marks in the order the work arrived.
            .OrderBy(submission => submission.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Submission>> ListAllAsync(
        SubmissionFilter filter,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilter(WithRelatedNames(_context.Submissions.AsNoTracking()), filter)
            .OrderByDescending(submission => submission.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Loads the student and the assignment (with its class and subject), which are
    /// the names both submission projections read.
    /// </summary>
    private static IQueryable<Submission> WithRelatedNames(IQueryable<Submission> query) =>
        query
            .Include(submission => submission.Student)
            .Include(submission => submission.Assignment).ThenInclude(assignment => assignment.ClassRoom)
            .Include(submission => submission.Assignment).ThenInclude(assignment => assignment.Subject);

    private static IQueryable<Submission> ApplyFilter(
        IQueryable<Submission> query,
        SubmissionFilter filter)
    {
        if (filter.AssignmentId is { } assignmentId)
        {
            query = query.Where(submission => submission.AssignmentId == assignmentId);
        }

        if (filter.ClassRoomId is { } classRoomId)
        {
            // Through the parent assignment: a submission has no class of its own.
            query = query.Where(submission => submission.Assignment.ClassRoomId == classRoomId);
        }

        if (filter.Status is { } status)
        {
            query = query.Where(submission => submission.Status == status);
        }

        return query;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: UniqueViolation };
}
