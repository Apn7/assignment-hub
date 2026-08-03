using AssignmentHub.Application.Interfaces;
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
}
