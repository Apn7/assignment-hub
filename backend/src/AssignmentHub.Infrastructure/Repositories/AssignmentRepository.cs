using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Assignments;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssignmentHub.Infrastructure.Repositories;

/// <inheritdoc cref="IAssignmentRepository"/>
public sealed class AssignmentRepository : IAssignmentRepository
{
    private readonly AppDbContext _context;

    public AssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Assignment?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Tracked, and no Include: the caller reads scalar fields and writes scalar
        // fields, so loading the related rows would be three joins for nothing.
        return _context.Assignments.SingleOrDefaultAsync(
            assignment => assignment.Id == id, cancellationToken);
    }

    public Task<Assignment?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return WithRelatedNames(_context.Assignments.AsNoTracking())
            .SingleOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);
    }

    public Task<Assignment?> GetVisibleToStudentAsync(
        Guid id,
        Guid classRoomId,
        CancellationToken cancellationToken = default)
    {
        // The visibility predicate is part of the WHERE clause, so a draft or
        // another class's assignment is never loaded and cannot be leaked by a
        // later mistake in the calling code.
        return WithRelatedNames(_context.Assignments.AsNoTracking())
            .Where(AssignmentQueries.VisibleToStudent(classRoomId))
            .SingleOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Assignment>> ListForTeacherAsync(
        Guid teacherId,
        AssignmentFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = WithRelatedNames(_context.Assignments.AsNoTracking())
            .Where(assignment => assignment.CreatedByTeacherId == teacherId);

        return await ApplyFilter(query, filter)
            .OrderByDescending(assignment => assignment.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Assignment>> ListVisibleToStudentAsync(
        Guid classRoomId,
        CancellationToken cancellationToken = default)
    {
        return await WithRelatedNames(_context.Assignments.AsNoTracking())
            .Where(AssignmentQueries.VisibleToStudent(classRoomId))
            // Nearest deadline first: what is due soonest is what a student needs.
            .OrderBy(assignment => assignment.Deadline)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Assignment>> ListAllAsync(
        AssignmentFilter filter,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilter(WithRelatedNames(_context.Assignments.AsNoTracking()), filter)
            .OrderByDescending(assignment => assignment.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Add(Assignment assignment) => _context.Assignments.Add(assignment);

    public void Remove(Assignment assignment) => _context.Assignments.Remove(assignment);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Loads the three related rows whose names <c>AssignmentResponse</c> carries.
    /// Every query that feeds a response goes through this.
    /// </summary>
    private static IQueryable<Assignment> WithRelatedNames(IQueryable<Assignment> query) =>
        query
            .Include(assignment => assignment.ClassRoom)
            .Include(assignment => assignment.Subject)
            .Include(assignment => assignment.CreatedByTeacher);

    /// <summary>
    /// Adds a WHERE clause per supplied filter. Built conditionally rather than as
    /// one expression with null checks in it, so an unfiltered request produces a
    /// plain query and Postgres can use the (ClassRoomId, Status) index.
    /// </summary>
    private static IQueryable<Assignment> ApplyFilter(IQueryable<Assignment> query, AssignmentFilter filter)
    {
        if (filter.ClassRoomId is { } classRoomId)
        {
            query = query.Where(assignment => assignment.ClassRoomId == classRoomId);
        }

        if (filter.SubjectId is { } subjectId)
        {
            query = query.Where(assignment => assignment.SubjectId == subjectId);
        }

        if (filter.Status is { } status)
        {
            query = query.Where(assignment => assignment.Status == status);
        }

        return query;
    }
}
