using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Admin;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;

namespace AssignmentHub.Application.Services;

/// <inheritdoc cref="IAdminManagementService"/>
public sealed class AdminManagementService : IAdminManagementService
{
    private readonly IUserRepository _users;
    private readonly IClassRoomRepository _classRooms;
    private readonly ISubjectRepository _subjects;
    private readonly ITeacherAssignmentRepository _teacherAssignments;
    private readonly IPasswordHasher _passwordHasher;

    public AdminManagementService(
        IUserRepository users,
        IClassRoomRepository classRooms,
        ISubjectRepository subjects,
        ITeacherAssignmentRepository teacherAssignments,
        IPasswordHasher passwordHasher)
    {
        _users = users;
        _classRooms = classRooms;
        _subjects = subjects;
        _teacherAssignments = teacherAssignments;
        _passwordHasher = passwordHasher;
    }

    // ── Users ────────────────────────────────────────────────────────────────

    public async Task<Result<UserResponse>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: false, out var role))
            return Result<UserResponse>.Unprocessable($"Role '{request.Role}' is not valid. Must be Admin, Teacher, or Student.");

        // Student REQUIRES classRoomId; Teacher/Admin must NOT have one.
        if (role == UserRole.Student && request.ClassRoomId is null)
            return Result<UserResponse>.Unprocessable("A student must be assigned to a class.");

        if (role != UserRole.Student && request.ClassRoomId is not null)
            return Result<UserResponse>.Unprocessable($"A {role} must not be assigned to a class.");

        // When classRoomId is specified, verify it exists.
        ClassRoom? classRoom = null;
        if (request.ClassRoomId is { } classRoomId)
        {
            classRoom = await _classRooms.GetByIdAsync(classRoomId, cancellationToken);
            if (classRoom is null)
                return Result<UserResponse>.Unprocessable("The specified class does not exist.");
        }

        // Normalise email to lowercase on create (closes the documented
        // case-sensitivity limitation).
        var email = request.Email.Trim().ToLowerInvariant();

        // Layer 1: pre-check for duplicate email.
        var existing = await _users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
            return Result<UserResponse>.Conflict($"A user with the email '{email}' already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = role,
            ClassRoomId = request.ClassRoomId,
            CreatedAt = DateTime.UtcNow
        };

        // Layer 2: unique-violation mapping at the database level.
        var added = await _users.TryAddAsync(user, cancellationToken);
        if (!added)
            return Result<UserResponse>.Conflict($"A user with the email '{email}' already exists.");

        return Result<UserResponse>.Success(new UserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            ClassRoomId = user.ClassRoomId,
            ClassRoomName = classRoom?.Name
        });
    }

    public async Task<Result<IReadOnlyList<UserResponse>>> ListUsersAsync(
        UserRole? roleFilter = null,
        CancellationToken cancellationToken = default)
    {
        var users = await _users.ListAsync(roleFilter, cancellationToken);

        var response = users.Select(u => new UserResponse
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role.ToString(),
            ClassRoomId = u.ClassRoomId,
            ClassRoomName = u.ClassRoom?.Name
        }).ToList();

        return Result<IReadOnlyList<UserResponse>>.Success(response);
    }

    // ── ClassRooms ───────────────────────────────────────────────────────────

    public async Task<Result<ClassRoomResponse>> CreateClassRoomAsync(
        CreateClassRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        if (await _classRooms.ExistsByNameAsync(name, cancellationToken))
            return Result<ClassRoomResponse>.Conflict($"A class named '{name}' already exists.");

        var classRoom = new ClassRoom
        {
            Id = Guid.NewGuid(),
            Name = name
        };

        await _classRooms.AddAsync(classRoom, cancellationToken);

        return Result<ClassRoomResponse>.Success(new ClassRoomResponse
        {
            Id = classRoom.Id,
            Name = classRoom.Name
        });
    }

    public async Task<Result<IReadOnlyList<ClassRoomResponse>>> ListClassRoomsAsync(
        CancellationToken cancellationToken = default)
    {
        var classRooms = await _classRooms.ListAsync(cancellationToken);

        var response = classRooms.Select(c => new ClassRoomResponse
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();

        return Result<IReadOnlyList<ClassRoomResponse>>.Success(response);
    }

    // ── Subjects ─────────────────────────────────────────────────────────────

    public async Task<Result<SubjectResponse>> CreateSubjectAsync(
        CreateSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        if (await _subjects.ExistsByNameAsync(name, cancellationToken))
            return Result<SubjectResponse>.Conflict($"A subject named '{name}' already exists.");

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = name
        };

        await _subjects.AddAsync(subject, cancellationToken);

        return Result<SubjectResponse>.Success(new SubjectResponse
        {
            Id = subject.Id,
            Name = subject.Name
        });
    }

    public async Task<Result<IReadOnlyList<SubjectResponse>>> ListSubjectsAsync(
        CancellationToken cancellationToken = default)
    {
        var subjects = await _subjects.ListAsync(cancellationToken);

        var response = subjects.Select(s => new SubjectResponse
        {
            Id = s.Id,
            Name = s.Name
        }).ToList();

        return Result<IReadOnlyList<SubjectResponse>>.Success(response);
    }

    // ── Teacher Assignments ──────────────────────────────────────────────────

    public async Task<Result<TeacherAssignmentAdminResponse>> CreateTeacherAssignmentAsync(
        CreateTeacherAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        // Verify the target user exists and is actually a Teacher.
        var teacher = await _users.GetByIdAsync(request.TeacherId, cancellationToken);
        if (teacher is null)
            return Result<TeacherAssignmentAdminResponse>.Unprocessable("The specified teacher does not exist.");
        if (teacher.Role != UserRole.Teacher)
            return Result<TeacherAssignmentAdminResponse>.Unprocessable(
                $"User '{teacher.FullName}' has role {teacher.Role} and cannot receive a teaching assignment.");

        // Verify the class exists.
        var classRoom = await _classRooms.GetByIdAsync(request.ClassRoomId, cancellationToken);
        if (classRoom is null)
            return Result<TeacherAssignmentAdminResponse>.Unprocessable("The specified class does not exist.");

        // Verify the subject exists.
        var subject = await _subjects.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject is null)
            return Result<TeacherAssignmentAdminResponse>.Unprocessable("The specified subject does not exist.");

        var entity = new TeacherAssignment
        {
            Id = Guid.NewGuid(),
            TeacherId = request.TeacherId,
            ClassRoomId = request.ClassRoomId,
            SubjectId = request.SubjectId
        };

        // TryAddAsync returns false when the unique triple index is violated.
        var added = await _teacherAssignments.TryAddAsync(entity, cancellationToken);
        if (!added)
            return Result<TeacherAssignmentAdminResponse>.Conflict(
                $"'{teacher.FullName}' is already assigned to {classRoom.Name} / {subject.Name}.");

        return Result<TeacherAssignmentAdminResponse>.Success(new TeacherAssignmentAdminResponse
        {
            Id = entity.Id,
            TeacherId = teacher.Id,
            TeacherName = teacher.FullName,
            ClassRoomId = classRoom.Id,
            ClassRoomName = classRoom.Name,
            SubjectId = subject.Id,
            SubjectName = subject.Name
        });
    }

    public async Task<Result<IReadOnlyList<TeacherAssignmentAdminResponse>>> ListTeacherAssignmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var entitlements = await _teacherAssignments.ListAllAsync(cancellationToken);

        var response = entitlements.Select(ta => new TeacherAssignmentAdminResponse
        {
            Id = ta.Id,
            TeacherId = ta.TeacherId,
            TeacherName = ta.Teacher.FullName,
            ClassRoomId = ta.ClassRoomId,
            ClassRoomName = ta.ClassRoom.Name,
            SubjectId = ta.SubjectId,
            SubjectName = ta.Subject.Name
        }).ToList();

        return Result<IReadOnlyList<TeacherAssignmentAdminResponse>>.Success(response);
    }
}
