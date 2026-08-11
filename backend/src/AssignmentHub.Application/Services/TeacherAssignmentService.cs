using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.TeacherAssignments;
using AssignmentHub.Application.Interfaces;

namespace AssignmentHub.Application.Services;

/// <inheritdoc cref="ITeacherAssignmentService"/>
public sealed class TeacherAssignmentService : ITeacherAssignmentService
{
    private readonly ITeacherAssignmentRepository _teacherAssignments;

    public TeacherAssignmentService(ITeacherAssignmentRepository teacherAssignments)
    {
        _teacherAssignments = teacherAssignments;
    }

    public async Task<Result<IReadOnlyList<TeacherAssignmentResponse>>> ListMineAsync(
        Guid teacherId,
        CancellationToken cancellationToken = default)
    {
        var assignments = await _teacherAssignments.ListForTeacherAsync(teacherId, cancellationToken);

        var response = assignments.Select(ta => new TeacherAssignmentResponse
        {
            ClassRoomId = ta.ClassRoomId,
            ClassRoomName = ta.ClassRoom?.Name ?? string.Empty,
            SubjectId = ta.SubjectId,
            SubjectName = ta.Subject?.Name ?? string.Empty
        }).ToList();

        return Result<IReadOnlyList<TeacherAssignmentResponse>>.Success(response);
    }
}
