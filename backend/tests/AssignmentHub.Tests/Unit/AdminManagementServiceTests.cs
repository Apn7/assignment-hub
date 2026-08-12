using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Admin;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Application.Services;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;

namespace AssignmentHub.Tests.Unit;

/// <summary>
/// Behaviour of the admin management service. Pure unit tests: no database, no HTTP.
/// Every collaborator is mocked.
/// </summary>
public class AdminManagementServiceTests
{
    private static readonly Guid ExistingClassId = new("40000000-0000-0000-0000-000000000001");
    private static readonly Guid ExistingSubjectId = new("50000000-0000-0000-0000-000000000001");
    private static readonly Guid TeacherUserId = new("20000000-0000-0000-0000-000000000001");

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IClassRoomRepository> _classRooms = new();
    private readonly Mock<ISubjectRepository> _subjects = new();
    private readonly Mock<ITeacherAssignmentRepository> _teacherAssignments = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    private AdminManagementService CreateSut() => new(
        _users.Object,
        _classRooms.Object,
        _subjects.Object,
        _teacherAssignments.Object,
        _passwordHasher.Object);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupClassRoomExists(Guid? id = null)
    {
        var cid = id ?? ExistingClassId;
        _classRooms
            .Setup(r => r.GetByIdAsync(cid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClassRoom { Id = cid, Name = "Class 9 – A" });
    }

    private void SetupSubjectExists(Guid? id = null)
    {
        var sid = id ?? ExistingSubjectId;
        _subjects
            .Setup(r => r.GetByIdAsync(sid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subject { Id = sid, Name = "Physics" });
    }

    private void SetupTeacherUser()
    {
        _users
            .Setup(r => r.GetByIdAsync(TeacherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = TeacherUserId,
                FullName = "Ayesha Rahman",
                Email = "teacher1@assignmenthub.local",
                Role = UserRole.Teacher
            });
    }

    private void SetupPasswordHasher()
    {
        _passwordHasher
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns("hashed-password");
    }

    private void SetupUserAddSucceeds()
    {
        _users
            .Setup(r => r.TryAddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CREATE USER
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateUser_Student_WithoutClassRoomId_Returns422()
    {
        var result = await CreateSut().CreateUserAsync(new CreateUserRequest
        {
            FullName = "Test Student",
            Email = "student@test.local",
            Password = "Password#1234",
            Role = "Student",
            ClassRoomId = null
        });

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Unprocessable);
        result.Error.Should().Contain("class");
    }

    [Fact]
    public async Task CreateUser_Teacher_WithClassRoomId_Returns422()
    {
        var result = await CreateSut().CreateUserAsync(new CreateUserRequest
        {
            FullName = "Test Teacher",
            Email = "teacher@test.local",
            Password = "Password#1234",
            Role = "Teacher",
            ClassRoomId = ExistingClassId
        });

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Unprocessable);
        result.Error.Should().Contain("must not be assigned");
    }

    [Fact]
    public async Task CreateUser_Admin_WithClassRoomId_Returns422()
    {
        var result = await CreateSut().CreateUserAsync(new CreateUserRequest
        {
            FullName = "Test Admin",
            Email = "admin@test.local",
            Password = "Password#1234",
            Role = "Admin",
            ClassRoomId = ExistingClassId
        });

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Unprocessable);
        result.Error.Should().Contain("must not be assigned");
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_PreCheck_Returns409()
    {
        SetupClassRoomExists();
        SetupPasswordHasher();

        _users
            .Setup(r => r.GetByEmailAsync("duplicate@test.local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Email = "duplicate@test.local",
                Role = UserRole.Student
            });

        var result = await CreateSut().CreateUserAsync(new CreateUserRequest
        {
            FullName = "Duplicate User",
            Email = "duplicate@test.local",
            Password = "Password#1234",
            Role = "Student",
            ClassRoomId = ExistingClassId
        });

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_DbViolation_Returns409()
    {
        SetupClassRoomExists();
        SetupPasswordHasher();

        // Pre-check passes (no existing user found)
        _users
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // But the database insert fails due to race condition
        _users
            .Setup(r => r.TryAddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateSut().CreateUserAsync(new CreateUserRequest
        {
            FullName = "Race Condition User",
            Email = "race@test.local",
            Password = "Password#1234",
            Role = "Student",
            ClassRoomId = ExistingClassId
        });

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateUser_NormalisesEmailToLowercase()
    {
        SetupClassRoomExists();
        SetupPasswordHasher();
        SetupUserAddSucceeds();

        _users
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateSut().CreateUserAsync(new CreateUserRequest
        {
            FullName = "Cased Email User",
            Email = "  UPPER@Test.Local  ",
            Password = "Password#1234",
            Role = "Student",
            ClassRoomId = ExistingClassId
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("upper@test.local");
    }

    [Fact]
    public async Task CreateUser_PasswordHashVerifiableByLoginPath()
    {
        // This test asserts the service delegates to IPasswordHasher.Hash with
        // the plaintext password, which is the same interface AuthService.Login
        // uses for verification — guaranteeing create-then-login works.
        SetupClassRoomExists();
        SetupUserAddSucceeds();

        const string plaintext = "Password#1234";
        _passwordHasher
            .Setup(h => h.Hash(plaintext))
            .Returns("pbkdf2-hash-of-plaintext");

        _users
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateSut().CreateUserAsync(new CreateUserRequest
        {
            FullName = "Hash Test",
            Email = "hash@test.local",
            Password = plaintext,
            Role = "Student",
            ClassRoomId = ExistingClassId
        });

        result.IsSuccess.Should().BeTrue();

        // Verify the hasher was called with the original plaintext.
        _passwordHasher.Verify(h => h.Hash(plaintext), Times.Once);

        // The user entity passed to the repository should carry the hash, not the plaintext.
        _users.Verify(r => r.TryAddAsync(
            It.Is<User>(u => u.PasswordHash == "pbkdf2-hash-of-plaintext"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateUser_Student_NonexistentClass_Returns422()
    {
        // ClassRoom does NOT exist
        _classRooms
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClassRoom?)null);

        var result = await CreateSut().CreateUserAsync(new CreateUserRequest
        {
            FullName = "Orphan Student",
            Email = "orphan@test.local",
            Password = "Password#1234",
            Role = "Student",
            ClassRoomId = Guid.NewGuid()
        });

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Unprocessable);
        result.Error.Should().Contain("class does not exist");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RESPONSE DTO SHAPE
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void UserResponse_DoesNotExposePasswordHash()
    {
        // Guards against someone "helpfully" adding a PasswordHash property.
        typeof(UserResponse).GetProperties()
            .Select(p => p.Name)
            .Should().NotContain(nameof(User.PasswordHash));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CREATE TEACHER ASSIGNMENT
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateTeacherAssignment_NonTeacherTarget_Returns422()
    {
        // Target user exists but is a Student, not a Teacher.
        _users
            .Setup(r => r.GetByIdAsync(TeacherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = TeacherUserId,
                FullName = "Student User",
                Email = "student@test.local",
                Role = UserRole.Student
            });

        SetupClassRoomExists();
        SetupSubjectExists();

        var result = await CreateSut().CreateTeacherAssignmentAsync(new CreateTeacherAssignmentRequest
        {
            TeacherId = TeacherUserId,
            ClassRoomId = ExistingClassId,
            SubjectId = ExistingSubjectId
        });

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Unprocessable);
        result.Error.Should().Contain("Student");
    }

    [Fact]
    public async Task CreateTeacherAssignment_DuplicateTriple_Returns409()
    {
        SetupTeacherUser();
        SetupClassRoomExists();
        SetupSubjectExists();

        // TryAddAsync returns false (unique index violation).
        _teacherAssignments
            .Setup(r => r.TryAddAsync(It.IsAny<TeacherAssignment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateSut().CreateTeacherAssignmentAsync(new CreateTeacherAssignmentRequest
        {
            TeacherId = TeacherUserId,
            ClassRoomId = ExistingClassId,
            SubjectId = ExistingSubjectId
        });

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("already assigned");
    }

    [Fact]
    public async Task CreateTeacherAssignment_NonexistentTeacher_Returns422()
    {
        _users
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateSut().CreateTeacherAssignmentAsync(new CreateTeacherAssignmentRequest
        {
            TeacherId = Guid.NewGuid(),
            ClassRoomId = ExistingClassId,
            SubjectId = ExistingSubjectId
        });

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Unprocessable);
        result.Error.Should().Contain("teacher does not exist");
    }

    [Fact]
    public async Task CreateTeacherAssignment_NonexistentClass_Returns422()
    {
        SetupTeacherUser();

        _classRooms
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClassRoom?)null);

        var result = await CreateSut().CreateTeacherAssignmentAsync(new CreateTeacherAssignmentRequest
        {
            TeacherId = TeacherUserId,
            ClassRoomId = Guid.NewGuid(),
            SubjectId = ExistingSubjectId
        });

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Unprocessable);
        result.Error.Should().Contain("class does not exist");
    }

    [Fact]
    public async Task CreateTeacherAssignment_NonexistentSubject_Returns422()
    {
        SetupTeacherUser();
        SetupClassRoomExists();

        _subjects
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subject?)null);

        var result = await CreateSut().CreateTeacherAssignmentAsync(new CreateTeacherAssignmentRequest
        {
            TeacherId = TeacherUserId,
            ClassRoomId = ExistingClassId,
            SubjectId = Guid.NewGuid()
        });

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Unprocessable);
        result.Error.Should().Contain("subject does not exist");
    }

    [Fact]
    public async Task CreateTeacherAssignment_ValidRequest_ReturnsSuccess()
    {
        SetupTeacherUser();
        SetupClassRoomExists();
        SetupSubjectExists();

        _teacherAssignments
            .Setup(r => r.TryAddAsync(It.IsAny<TeacherAssignment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateSut().CreateTeacherAssignmentAsync(new CreateTeacherAssignmentRequest
        {
            TeacherId = TeacherUserId,
            ClassRoomId = ExistingClassId,
            SubjectId = ExistingSubjectId
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.TeacherName.Should().Be("Ayesha Rahman");
        result.Value!.ClassRoomName.Should().Be("Class 9 – A");
        result.Value!.SubjectName.Should().Be("Physics");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CREATE CLASSROOM
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateClassRoom_DuplicateName_Returns409()
    {
        _classRooms
            .Setup(r => r.ExistsByNameAsync("Physics Lab", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateSut().CreateClassRoomAsync(new CreateClassRoomRequest
        {
            Name = "Physics Lab"
        });

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("already exists");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CREATE SUBJECT
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateSubject_DuplicateName_Returns409()
    {
        _subjects
            .Setup(r => r.ExistsByNameAsync("Chemistry", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateSut().CreateSubjectAsync(new CreateSubjectRequest
        {
            Name = "Chemistry"
        });

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("already exists");
    }
}
