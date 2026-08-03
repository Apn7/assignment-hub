using AssignmentHub.Application.DTOs.Submissions;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;

namespace AssignmentHub.Tests.TestDoubles;

/// <summary>
/// In-memory <see cref="ISubmissionRepository"/> backed by a plain list.
/// </summary>
/// <remarks>
/// Hand-written for the same reason as
/// <see cref="FakeAssignmentRepository"/>: these tests care about the behaviour of
/// a store, not about which methods were called. Grade-then-regrade has to see its
/// own first write, and a duplicate submit has to be refused because a row is
/// really there.
///
/// <see cref="RejectNextAdd"/> is the seam for the one thing a list cannot model —
/// the unique index refusing an insert that the service's own check had already
/// waved through, which is only reachable when two of the student's requests race.
/// </remarks>
internal sealed class FakeSubmissionRepository : ISubmissionRepository
{
    private readonly List<Submission> _submissions;

    public FakeSubmissionRepository(params Submission[] seed)
    {
        _submissions = seed.ToList();
    }

    public IReadOnlyList<Submission> Items => _submissions;

    /// <summary>How many times the service committed. Zero proves a rejection wrote nothing.</summary>
    public int SaveCount { get; private set; }

    /// <summary>
    /// Makes the next <see cref="TryAddAsync"/> report a unique violation, standing
    /// in for a concurrent request that inserted first.
    /// </summary>
    public bool RejectNextAdd { get; set; }

    public Task<bool> TryAddAsync(Submission submission, CancellationToken cancellationToken = default)
    {
        if (RejectNextAdd)
        {
            RejectNextAdd = false;
            return Task.FromResult(false);
        }

        _submissions.Add(submission);
        SaveCount++;
        return Task.FromResult(true);
    }

    public Task<bool> ExistsForAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_submissions.Any(
            submission => submission.AssignmentId == assignmentId && submission.StudentId == studentId));

    public Task<Submission?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_submissions.SingleOrDefault(submission => submission.Id == id));

    public Task<Submission?> GetOwnForUpdateAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_submissions.SingleOrDefault(
            submission => submission.AssignmentId == assignmentId && submission.StudentId == studentId));

    public Task<Submission?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetForUpdateAsync(id, cancellationToken);

    public Task<Submission?> GetOwnDetailAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        GetOwnForUpdateAsync(assignmentId, studentId, cancellationToken);

    public Task<IReadOnlyList<Submission>> ListForAssignmentAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Submission>>(_submissions
            .Where(submission => submission.AssignmentId == assignmentId)
            .OrderBy(submission => submission.SubmittedAt)
            .ToList());

    public Task<IReadOnlyList<Submission>> ListAllAsync(
        SubmissionFilter filter,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Submission> source = _submissions;

        if (filter.AssignmentId is { } assignmentId)
        {
            source = source.Where(submission => submission.AssignmentId == assignmentId);
        }

        if (filter.ClassRoomId is { } classRoomId)
        {
            source = source.Where(submission => submission.Assignment?.ClassRoomId == classRoomId);
        }

        if (filter.Status is { } status)
        {
            source = source.Where(submission => submission.Status == status);
        }

        return Task.FromResult<IReadOnlyList<Submission>>(
            source.OrderByDescending(submission => submission.SubmittedAt).ToList());
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
