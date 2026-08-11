using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssignmentHub.Infrastructure.Repositories;

/// <inheritdoc cref="ITeacherAssignmentRepository"/>
public sealed class TeacherAssignmentRepository : ITeacherAssignmentRepository
{
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
}
