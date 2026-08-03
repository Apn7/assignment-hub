using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Assignments;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;

namespace AssignmentHub.Tests.TestDoubles;

/// <summary>
/// In-memory <see cref="IAssignmentRepository"/> backed by a plain list.
/// </summary>
/// <remarks>
/// Hand-written rather than mocked because the service tests care about the
/// behaviour of a store, not about which methods were called: publish then
/// publish again has to see its own first write, and a delete has to actually
/// remove something.
///
/// The student queries run the real
/// <see cref="AssignmentQueries.VisibleToStudent"/> expression, so a service test
/// asserting that drafts are invisible is asserting it against the same predicate
/// the database is given. The teacher and admin filters are re-implemented here in
/// LINQ-to-objects; those are narrowing conveniences rather than a security
/// boundary, and their SQL form is covered by
/// <c>Persistence/AssignmentRepositoryTests</c>.
/// </remarks>
internal sealed class FakeAssignmentRepository : IAssignmentRepository
{
    private readonly List<Assignment> _assignments;

    public FakeAssignmentRepository(params Assignment[] seed)
    {
        _assignments = seed.ToList();
    }

    /// <summary>Everything currently stored, so a test can inspect what was written.</summary>
    public IReadOnlyList<Assignment> Items => _assignments;

    /// <summary>How many times the service committed. Zero proves a rejection wrote nothing.</summary>
    public int SaveCount { get; private set; }

    public Task<Assignment?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_assignments.SingleOrDefault(assignment => assignment.Id == id));

    public Task<Assignment?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_assignments.SingleOrDefault(assignment => assignment.Id == id));

    public Task<Assignment?> GetVisibleToStudentAsync(
        Guid id,
        Guid classRoomId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_assignments
            .Where(AssignmentQueries.VisibleToStudent(classRoomId).Compile())
            .SingleOrDefault(assignment => assignment.Id == id));

    public Task<IReadOnlyList<Assignment>> ListVisibleToStudentAsync(
        Guid classRoomId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Assignment>>(_assignments
            .Where(AssignmentQueries.VisibleToStudent(classRoomId).Compile())
            .OrderBy(assignment => assignment.Deadline)
            .ToList());

    public Task<IReadOnlyList<Assignment>> ListForTeacherAsync(
        Guid teacherId,
        AssignmentFilter filter,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Assignment>>(Filter(
                _assignments.Where(assignment => assignment.CreatedByTeacherId == teacherId), filter)
            .OrderByDescending(assignment => assignment.CreatedAt)
            .ToList());

    public Task<IReadOnlyList<Assignment>> ListAllAsync(
        AssignmentFilter filter,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Assignment>>(Filter(_assignments, filter)
            .OrderByDescending(assignment => assignment.CreatedAt)
            .ToList());

    public void Add(Assignment assignment) => _assignments.Add(assignment);

    public void Remove(Assignment assignment) => _assignments.Remove(assignment);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    private static IEnumerable<Assignment> Filter(IEnumerable<Assignment> source, AssignmentFilter filter)
    {
        if (filter.ClassRoomId is { } classRoomId)
        {
            source = source.Where(assignment => assignment.ClassRoomId == classRoomId);
        }

        if (filter.SubjectId is { } subjectId)
        {
            source = source.Where(assignment => assignment.SubjectId == subjectId);
        }

        if (filter.Status is { } status)
        {
            source = source.Where(assignment => assignment.Status == status);
        }

        return source;
    }
}
