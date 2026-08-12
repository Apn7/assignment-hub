using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AssignmentHub.Infrastructure.Repositories;

/// <inheritdoc cref="ITeacherAssignmentRepository"/>
public sealed class TeacherAssignmentRepository : ITeacherAssignmentRepository
{
    /// <summary>
    /// PostgreSQL's SQLSTATE for unique_violation. The unique index on
    /// (TeacherId, ClassRoomId, SubjectId) means a 23505 here is a duplicate
    /// entitlement.
    /// </summary>
    private const string UniqueViolation = "23505";

    private readonly AppDbContext _context;

    public TeacherAssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(
        Guid teacherId,
        Guid classRoomId,
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        // Matches the unique index on (TeacherId, ClassRoomId, SubjectId), so this
        // is an index probe rather than a scan.
        return _context.TeacherAssignments.AnyAsync(
            teacherAssignment => teacherAssignment.TeacherId == teacherId
                                 && teacherAssignment.ClassRoomId == classRoomId
                                 && teacherAssignment.SubjectId == subjectId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<TeacherAssignment>> ListForTeacherAsync(
        Guid teacherId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TeacherAssignments
            .AsNoTracking()
            .Include(ta => ta.ClassRoom)
            .Include(ta => ta.Subject)
            .Where(ta => ta.TeacherId == teacherId)
            .OrderBy(ta => ta.ClassRoom.Name)
            .ThenBy(ta => ta.Subject.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryAddAsync(
        TeacherAssignment entity,
        CancellationToken cancellationToken = default)
    {
        _context.TeacherAssignments.Add(entity);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _context.Entry(entity).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<IReadOnlyList<TeacherAssignment>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.TeacherAssignments
            .AsNoTracking()
            .Include(ta => ta.Teacher)
            .Include(ta => ta.ClassRoom)
            .Include(ta => ta.Subject)
            .OrderBy(ta => ta.Teacher.FullName)
            .ThenBy(ta => ta.ClassRoom.Name)
            .ThenBy(ta => ta.Subject.Name)
            .ToListAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: UniqueViolation };
}

