using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.TeacherAssignments;

namespace AssignmentHub.Application.Interfaces;

/// <summary>
/// Application service for reading teacher entitlements.
/// </summary>
public interface ITeacherAssignmentService
{
    /// <summary>
    /// Lists all teaching pairs assigned to the specified teacher.
    /// </summary>
    Task<Result<IReadOnlyList<TeacherAssignmentResponse>>> ListMineAsync(
        Guid teacherId,
        CancellationToken cancellationToken = default);
}
