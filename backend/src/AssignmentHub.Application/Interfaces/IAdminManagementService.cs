using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Admin;
using AssignmentHub.Domain.Enums;

namespace AssignmentHub.Application.Interfaces;

/// <summary>
/// Admin management operations: create and list users, classrooms, subjects,
/// and teacher-assignment entitlements.
/// </summary>
public interface IAdminManagementService
{
    // ── Users ────────────────────────────────────────────────────────────────

    Task<Result<UserResponse>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<UserResponse>>> ListUsersAsync(
        UserRole? roleFilter = null,
        CancellationToken cancellationToken = default);

    // ── ClassRooms ───────────────────────────────────────────────────────────

    Task<Result<ClassRoomResponse>> CreateClassRoomAsync(
        CreateClassRoomRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ClassRoomResponse>>> ListClassRoomsAsync(
        CancellationToken cancellationToken = default);

    // ── Subjects ─────────────────────────────────────────────────────────────

    Task<Result<SubjectResponse>> CreateSubjectAsync(
        CreateSubjectRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SubjectResponse>>> ListSubjectsAsync(
        CancellationToken cancellationToken = default);

    // ── Teacher Assignments ──────────────────────────────────────────────────

    Task<Result<TeacherAssignmentAdminResponse>> CreateTeacherAssignmentAsync(
        CreateTeacherAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<TeacherAssignmentAdminResponse>>> ListTeacherAssignmentsAsync(
        CancellationToken cancellationToken = default);
}
