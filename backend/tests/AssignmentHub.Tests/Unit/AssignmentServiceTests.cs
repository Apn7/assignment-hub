using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Assignments;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Application.Services;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;
using AssignmentHub.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssignmentHub.Tests.Unit;

/// <summary>
/// The eight assignment business rules, one or more tests each. No database, no
/// HTTP: the store is a list and the clock is frozen, so every assertion is about
/// a rule rather than about infrastructure.
/// </summary>
public class AssignmentServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid Teacher1 = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid Teacher2 = new("20000000-0000-0000-0000-000000000002");
    private static readonly Guid Student9A = new("30000000-0000-0000-0000-000000000001");
    private static readonly Guid Student10A = new("30000000-0000-0000-0000-000000000003");
    private static readonly Guid Class9A = new("40000000-0000-0000-0000-000000000001");
    private static readonly Guid Class10A = new("40000000-0000-0000-0000-000000000002");
    private static readonly Guid Physics = new("50000000-0000-0000-0000-000000000001");
    private static readonly Guid English = new("50000000-0000-0000-0000-000000000003");

    private readonly Mock<ITeacherAssignmentRepository> _teacherAssignments = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(Now));

    // ---------------------------------------------------------------------
    // Rule 1: a teacher may only create work for a class/subject pair they hold
    // ---------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ForAClassAndSubjectTheTeacherDoesNotTeach_IsForbidden()
    {
        var repository = new FakeAssignmentRepository();

        // No teacher assignment is set up, so the check returns false.
        var result = await CreateSut(repository).CreateAsync(Teacher1, CreateRequest(Class10A, English));

        result.Status.Should().Be(ResultStatus.Forbidden);
        repository.Items.Should().BeEmpty();
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_ForAPairTheTeacherHolds_StartsAsADraft()
    {
        var repository = new FakeAssignmentRepository();
        AllowTeaching(Teacher1, Class9A, Physics);

        var result = await CreateSut(repository).CreateAsync(Teacher1, CreateRequest(Class9A, Physics));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(AssignmentStatus.Draft));
        result.Value.CreatedByTeacherId.Should().Be(Teacher1);

        // Nothing a client sends can make an assignment born published; publishing is
        // a separate action.
        repository.Items.Should().ContainSingle()
            .Which.Status.Should().Be(AssignmentStatus.Draft);
    }

    [Fact]
    public async Task CreateAsync_StampsCreatedAndUpdatedFromTheInjectedClock()
    {
        var repository = new FakeAssignmentRepository();
        AllowTeaching(Teacher1, Class9A, Physics);

        await CreateSut(repository).CreateAsync(Teacher1, CreateRequest(Class9A, Physics));

        var stored = repository.Items.Should().ContainSingle().Subject;
        stored.CreatedAt.Should().Be(Now);
        stored.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public async Task CreateAsync_ReadsADeadlineWithoutATimezoneAsUtc()
    {
        var repository = new FakeAssignmentRepository();
        AllowTeaching(Teacher1, Class9A, Physics);

        var withoutTimezone = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Unspecified);

        await CreateSut(repository).CreateAsync(
            Teacher1,
            CreateRequest(Class9A, Physics, deadline: withoutTimezone));

        var stored = repository.Items.Should().ContainSingle().Subject.Deadline;

        // Kind matters: a value the clock is later compared against must not be
        // ambiguous, and Npgsql rejects a non-UTC DateTime outright.
        stored.Kind.Should().Be(DateTimeKind.Utc);
        stored.Should().Be(withoutTimezone, "the wall-clock reading is kept, only labelled");
    }

    // ---------------------------------------------------------------------
    // Rule 2: a teacher may only act on their own assignments
    // ---------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_OnAnotherTeachersAssignment_IsNotFound()
    {
        var assignment = Draft(Teacher1, Class9A, Physics);
        var repository = new FakeAssignmentRepository(assignment);
        AllowTeaching(Teacher2, Class9A, Physics);

        var result = await CreateSut(repository).UpdateAsync(
            Teacher2, assignment.Id, UpdateRequest(assignment, title: "Rewritten by a colleague"));

        // Not Forbidden: 403 would confirm the id names a real assignment.
        result.Status.Should().Be(ResultStatus.NotFound);
        assignment.Title.Should().NotBe("Rewritten by a colleague");
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task PublishAsync_OnAnotherTeachersAssignment_IsNotFound()
    {
        var assignment = Draft(Teacher1, Class9A, Physics);
        var repository = new FakeAssignmentRepository(assignment);

        var result = await CreateSut(repository).PublishAsync(Teacher2, assignment.Id);

        result.Status.Should().Be(ResultStatus.NotFound);
        assignment.Status.Should().Be(AssignmentStatus.Draft);
    }

    [Fact]
    public async Task DeleteAsync_OnAnotherTeachersAssignment_IsNotFound()
    {
        var assignment = Draft(Teacher1, Class9A, Physics);
        var repository = new FakeAssignmentRepository(assignment);

        var result = await CreateSut(repository).DeleteAsync(Teacher2, assignment.Id);

        result.Status.Should().Be(ResultStatus.NotFound);
        repository.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task NotFoundOutcomes_AllCarryTheSameMessage()
    {
        var owned = Draft(Teacher1, Class9A, Physics);
        var otherClass = Published(Teacher2, Class10A, English, deadline: Now.AddDays(5));
        var repository = new FakeAssignmentRepository(owned, otherClass);
        var sut = CreateSut(repository);

        StudentIn(Student9A, Class9A);

        var absent = await sut.PublishAsync(Teacher1, Guid.NewGuid());
        var notMine = await sut.PublishAsync(Teacher2, owned.Id);
        var draftProbedByStudent = await sut.GetForStudentAsync(Student9A, owned.Id);
        var otherClassProbe = await sut.GetForStudentAsync(Student9A, otherClass.Id);

        // Four different reasons, one indistinguishable answer. Anything else lets a
        // caller map out what exists by comparing bodies.
        var messages = new[]
        {
            absent.Error, notMine.Error, draftProbedByStudent.Error, otherClassProbe.Error
        };

        messages.Should().OnlyContain(message => message == "Assignment not found.");
    }

    // ---------------------------------------------------------------------
    // Rule 3: maximum marks must be a sensible positive number
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(AssignmentRules.MaxAllowedMarks + 1)]
    public async Task CreateAsync_WithMaxMarksOutsideTheAllowedRange_IsRejected(int maxMarks)
    {
        var repository = new FakeAssignmentRepository();
        AllowTeaching(Teacher1, Class9A, Physics);

        var result = await CreateSut(repository).CreateAsync(
            Teacher1, CreateRequest(Class9A, Physics, maxMarks: maxMarks));

        result.Status.Should().Be(ResultStatus.ValidationFailed);
        repository.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(AssignmentRules.MinMaxMarks)]
    [InlineData(AssignmentRules.MaxAllowedMarks)]
    public async Task CreateAsync_AtTheBoundsOfTheAllowedRange_IsAccepted(int maxMarks)
    {
        var repository = new FakeAssignmentRepository();
        AllowTeaching(Teacher1, Class9A, Physics);

        var result = await CreateSut(repository).CreateAsync(
            Teacher1, CreateRequest(Class9A, Physics, maxMarks: maxMarks));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithMaxMarksOutsideTheAllowedRange_IsRejected()
    {
        var assignment = Draft(Teacher1, Class9A, Physics);
        var repository = new FakeAssignmentRepository(assignment);

        var result = await CreateSut(repository).UpdateAsync(
            Teacher1, assignment.Id, UpdateRequest(assignment, maxMarks: 0));

        result.Status.Should().Be(ResultStatus.ValidationFailed);
        repository.SaveCount.Should().Be(0);
    }

    // ---------------------------------------------------------------------
    // Rule 4: publishing requires a deadline still in the future
    // ---------------------------------------------------------------------

    [Fact]
    public async Task PublishAsync_WhenTheDeadlineHasPassed_IsRejected()
    {
        var assignment = Draft(Teacher1, Class9A, Physics, deadline: Now.AddDays(-1));
        var repository = new FakeAssignmentRepository(assignment);

        var result = await CreateSut(repository).PublishAsync(Teacher1, assignment.Id);

        result.Status.Should().Be(ResultStatus.Conflict);
        assignment.Status.Should().Be(AssignmentStatus.Draft);
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task PublishAsync_WhenTheDeadlineIsExactlyNow_IsRejected()
    {
        // A deadline of "right now" gives students no time at all, so the rule is
        // strictly-in-the-future rather than not-yet-past.
        var assignment = Draft(Teacher1, Class9A, Physics, deadline: Now);
        var repository = new FakeAssignmentRepository(assignment);

        var result = await CreateSut(repository).PublishAsync(Teacher1, assignment.Id);

        result.Status.Should().Be(ResultStatus.Conflict);
    }

    [Fact]
    public async Task PublishAsync_WithAFutureDeadline_FlipsTheStatus()
    {
        var assignment = Draft(Teacher1, Class9A, Physics, deadline: Now.AddDays(7));
        var repository = new FakeAssignmentRepository(assignment);

        var result = await CreateSut(repository).PublishAsync(Teacher1, assignment.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(AssignmentStatus.Published));
        assignment.Status.Should().Be(AssignmentStatus.Published);
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task PublishAsync_WhenAlreadyPublished_IsRejectedRatherThanIdempotent()
    {
        var assignment = Draft(Teacher1, Class9A, Physics, deadline: Now.AddDays(7));
        var repository = new FakeAssignmentRepository(assignment);
        var sut = CreateSut(repository);

        await sut.PublishAsync(Teacher1, assignment.Id);
        var second = await sut.PublishAsync(Teacher1, assignment.Id);

        // A deliberate choice, documented in docs/assignments.md: publishing is an
        // event, so a duplicate is a client bug worth surfacing, not a no-op.
        second.Status.Should().Be(ResultStatus.Conflict);
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task PublishAsync_AfterTheDeadlineIsExtended_Succeeds()
    {
        var assignment = Draft(Teacher1, Class9A, Physics, deadline: Now.AddDays(-1));
        var repository = new FakeAssignmentRepository(assignment);
        var sut = CreateSut(repository);

        (await sut.PublishAsync(Teacher1, assignment.Id)).Status.Should().Be(ResultStatus.Conflict);

        // The error message tells the teacher to extend the deadline; this proves that
        // advice actually works.
        await sut.UpdateAsync(
            Teacher1, assignment.Id, UpdateRequest(assignment, deadline: Now.AddDays(10)));

        (await sut.PublishAsync(Teacher1, assignment.Id)).IsSuccess.Should().BeTrue();
    }

    // ---------------------------------------------------------------------
    // Rule 5: what a published assignment may still change
    // ---------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_OnAPublishedAssignment_CanRewordItAndExtendTheDeadline()
    {
        var assignment = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(3));
        var repository = new FakeAssignmentRepository(assignment);

        var result = await CreateSut(repository).UpdateAsync(
            Teacher1,
            assignment.Id,
            UpdateRequest(assignment, title: "Clarified wording", deadline: Now.AddDays(10)));

        result.IsSuccess.Should().BeTrue();
        assignment.Title.Should().Be("Clarified wording");
        assignment.Deadline.Should().Be(Now.AddDays(10));
    }

    [Fact]
    public async Task UpdateAsync_OnAPublishedAssignment_CannotBringTheDeadlineForward()
    {
        var assignment = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(7));
        var repository = new FakeAssignmentRepository(assignment);

        var result = await CreateSut(repository).UpdateAsync(
            Teacher1, assignment.Id, UpdateRequest(assignment, deadline: Now.AddDays(2)));

        result.Status.Should().Be(ResultStatus.Conflict);
        assignment.Deadline.Should().Be(Now.AddDays(7));
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_OnAPublishedAssignment_AcceptsAnUnchangedDeadline()
    {
        var assignment = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(7));
        var repository = new FakeAssignmentRepository(assignment);

        // An edit form round-trips the current deadline. That must not read as an
        // attempt to move it.
        var result = await CreateSut(repository).UpdateAsync(
            Teacher1, assignment.Id, UpdateRequest(assignment, title: "Typo fixed"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_OnAPublishedAssignment_CannotChangeMaxMarks()
    {
        var assignment = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(7), maxMarks: 50);
        var repository = new FakeAssignmentRepository(assignment);

        var result = await CreateSut(repository).UpdateAsync(
            Teacher1, assignment.Id, UpdateRequest(assignment, maxMarks: 100));

        result.Status.Should().Be(ResultStatus.Conflict);
        assignment.MaxMarks.Should().Be(50);
    }

    [Fact]
    public async Task UpdateAsync_OnAPublishedAssignment_CannotChangeTheClassOrSubject()
    {
        var assignment = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(7));
        var repository = new FakeAssignmentRepository(assignment);
        AllowTeaching(Teacher1, Class10A, Physics);

        // Even though the teacher legitimately teaches the target pair: moving
        // published work would make it appear and disappear for two whole classes.
        var result = await CreateSut(repository).UpdateAsync(
            Teacher1, assignment.Id, UpdateRequest(assignment, classRoomId: Class10A));

        result.Status.Should().Be(ResultStatus.Conflict);
        assignment.ClassRoomId.Should().Be(Class9A);
    }

    // ---------------------------------------------------------------------
    // Rule 1 again, on the update path
    // ---------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_MovingADraftToAPairTheTeacherDoesNotTeach_IsForbidden()
    {
        var assignment = Draft(Teacher1, Class9A, Physics);
        var repository = new FakeAssignmentRepository(assignment);
        AllowTeaching(Teacher1, Class9A, Physics);

        // Creating a legitimate draft and then re-pointing it would otherwise walk
        // straight around the create-time entitlement check.
        var result = await CreateSut(repository).UpdateAsync(
            Teacher1, assignment.Id, UpdateRequest(assignment, classRoomId: Class10A, subjectId: English));

        result.Status.Should().Be(ResultStatus.Forbidden);
        assignment.ClassRoomId.Should().Be(Class9A);
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_MovingADraftToAPairTheTeacherDoesTeach_Succeeds()
    {
        var assignment = Draft(Teacher1, Class9A, Physics);
        var repository = new FakeAssignmentRepository(assignment);
        AllowTeaching(Teacher1, Class10A, English);

        var result = await CreateSut(repository).UpdateAsync(
            Teacher1, assignment.Id, UpdateRequest(assignment, classRoomId: Class10A, subjectId: English));

        result.IsSuccess.Should().BeTrue();
        assignment.ClassRoomId.Should().Be(Class10A);
        assignment.SubjectId.Should().Be(English);
    }

    // ---------------------------------------------------------------------
    // Rule 6: published never goes back to draft
    // ---------------------------------------------------------------------

    [Fact]
    public void UpdateRequest_HasNoStatusField()
    {
        // The structural half of the guard: no caller can express "make this a draft
        // again", so no future controller action can accidentally offer it.
        typeof(UpdateAssignmentRequest).GetProperties()
            .Select(property => property.Name)
            .Should().NotContain(nameof(Assignment.Status));
    }

    [Fact]
    public async Task UpdateAsync_OnAPublishedAssignment_LeavesItPublished()
    {
        var assignment = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(7));
        var repository = new FakeAssignmentRepository(assignment);

        var result = await CreateSut(repository).UpdateAsync(
            Teacher1, assignment.Id, UpdateRequest(assignment, title: "Reworded"));

        result.Value!.Status.Should().Be(nameof(AssignmentStatus.Published));
        assignment.Status.Should().Be(AssignmentStatus.Published);
    }

    // ---------------------------------------------------------------------
    // Rule 7: only drafts can be deleted
    // ---------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_OnADraft_RemovesIt()
    {
        var assignment = Draft(Teacher1, Class9A, Physics);
        var repository = new FakeAssignmentRepository(assignment);

        var result = await CreateSut(repository).DeleteAsync(Teacher1, assignment.Id);

        result.IsSuccess.Should().BeTrue();
        repository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_OnAPublishedAssignment_IsRejected()
    {
        var assignment = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(7));
        var repository = new FakeAssignmentRepository(assignment);

        var result = await CreateSut(repository).DeleteAsync(Teacher1, assignment.Id);

        result.Status.Should().Be(ResultStatus.Conflict);
        repository.Items.Should().ContainSingle();
    }

    // ---------------------------------------------------------------------
    // Rule 8: drafts and other classes are invisible to students
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ListForStudentAsync_ExcludesDraftsAndOtherClasses()
    {
        var visible = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(5));
        var draftInOwnClass = Draft(Teacher1, Class9A, Physics);
        var otherClass = Published(Teacher2, Class10A, English, deadline: Now.AddDays(2));

        var repository = new FakeAssignmentRepository(visible, draftInOwnClass, otherClass);
        StudentIn(Student9A, Class9A);

        var result = await CreateSut(repository).ListForStudentAsync(Student9A);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(assignment => assignment.Id).Should().Equal(visible.Id);
    }

    [Fact]
    public async Task ListForStudentAsync_IsOrderedByNearestDeadlineFirst()
    {
        var later = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(20));
        var sooner = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(2));

        var repository = new FakeAssignmentRepository(later, sooner);
        StudentIn(Student9A, Class9A);

        var result = await CreateSut(repository).ListForStudentAsync(Student9A);

        result.Value!.Select(assignment => assignment.Id).Should().Equal(sooner.Id, later.Id);
    }

    [Fact]
    public async Task ListForStudentAsync_UsesTheStoredClassNotTheCallersClaim()
    {
        var nineA = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(5));
        var tenA = Published(Teacher2, Class10A, English, deadline: Now.AddDays(5));

        var repository = new FakeAssignmentRepository(nineA, tenA);
        StudentIn(Student10A, Class10A);

        var result = await CreateSut(repository).ListForStudentAsync(Student10A);

        // The endpoint takes no class parameter at all; the class is looked up per
        // request so an admin moving a student takes effect without a fresh login.
        result.Value!.Select(assignment => assignment.Id).Should().Equal(tenA.Id);
        _users.Verify(
            users => users.GetByIdAsync(Student10A, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListForStudentAsync_WhenTheStudentBelongsToNoClass_IsEmpty()
    {
        var repository = new FakeAssignmentRepository(
            Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(5)));

        _users.Setup(users => users.GetByIdAsync(Student9A, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Student9A, Role = UserRole.Student, ClassRoomId = null });

        var result = await CreateSut(repository).ListForStudentAsync(Student9A);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetForStudentAsync_OnADraftInTheirOwnClass_IsNotFound()
    {
        var draft = Draft(Teacher1, Class9A, Physics);
        var repository = new FakeAssignmentRepository(draft);
        StudentIn(Student9A, Class9A);

        var result = await CreateSut(repository).GetForStudentAsync(Student9A, draft.Id);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task GetForStudentAsync_OnAnotherClassesPublishedAssignment_IsNotFound()
    {
        var otherClass = Published(Teacher2, Class10A, English, deadline: Now.AddDays(5));
        var repository = new FakeAssignmentRepository(otherClass);
        StudentIn(Student9A, Class9A);

        var result = await CreateSut(repository).GetForStudentAsync(Student9A, otherClass.Id);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task GetForStudentAsync_OnAPublishedAssignmentForTheirClass_Succeeds()
    {
        var visible = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(5));
        var repository = new FakeAssignmentRepository(visible);
        StudentIn(Student9A, Class9A);

        var result = await CreateSut(repository).GetForStudentAsync(Student9A, visible.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(visible.Id);
        result.Value.MaxMarks.Should().Be(visible.MaxMarks);
    }

    // ---------------------------------------------------------------------
    // Listings
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ListForTeacherAsync_ReturnsBothStatusesButOnlyTheirOwn()
    {
        var ownDraft = Draft(Teacher1, Class9A, Physics);
        var ownPublished = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(5));
        var colleagues = Published(Teacher2, Class10A, English, deadline: Now.AddDays(5));

        var repository = new FakeAssignmentRepository(ownDraft, ownPublished, colleagues);

        var result = await CreateSut(repository).ListForTeacherAsync(Teacher1, AssignmentFilter.None);

        result.Value!.Select(assignment => assignment.Id)
            .Should().BeEquivalentTo(new[] { ownDraft.Id, ownPublished.Id });
    }

    [Fact]
    public async Task ListForTeacherAsync_AppliesTheStatusFilter()
    {
        var ownDraft = Draft(Teacher1, Class9A, Physics);
        var ownPublished = Published(Teacher1, Class9A, Physics, deadline: Now.AddDays(5));
        var repository = new FakeAssignmentRepository(ownDraft, ownPublished);

        var result = await CreateSut(repository).ListForTeacherAsync(
            Teacher1, new AssignmentFilter { Status = AssignmentStatus.Draft });

        result.Value!.Select(assignment => assignment.Id).Should().Equal(ownDraft.Id);
    }

    [Fact]
    public async Task ListAllAsync_SeesEveryTeachersWorkIncludingDrafts()
    {
        var draft = Draft(Teacher1, Class9A, Physics);
        var published = Published(Teacher2, Class10A, English, deadline: Now.AddDays(5));
        var repository = new FakeAssignmentRepository(draft, published);

        var result = await CreateSut(repository).ListAllAsync(AssignmentFilter.None);

        result.Value!.Select(assignment => assignment.Id)
            .Should().BeEquivalentTo(new[] { draft.Id, published.Id });
    }

    // ---------------------------------------------------------------------
    // Fixtures
    // ---------------------------------------------------------------------

    private AssignmentService CreateSut(FakeAssignmentRepository assignments) => new(
        assignments,
        _teacherAssignments.Object,
        _users.Object,
        _clock,
        NullLogger<AssignmentService>.Instance);

    /// <summary>Grants the teacher entitlement to one class/subject pair.</summary>
    private void AllowTeaching(Guid teacherId, Guid classRoomId, Guid subjectId) =>
        _teacherAssignments
            .Setup(repository => repository.ExistsAsync(
                teacherId, classRoomId, subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

    private void StudentIn(Guid studentId, Guid classRoomId) =>
        _users
            .Setup(users => users.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = studentId,
                Role = UserRole.Student,
                ClassRoomId = classRoomId
            });

    private static CreateAssignmentRequest CreateRequest(
        Guid classRoomId,
        Guid subjectId,
        DateTime? deadline = null,
        int maxMarks = 20) => new()
    {
        Title = "Newton's Laws of Motion",
        Description = "Problems 1-10 from chapter 4.",
        ClassRoomId = classRoomId,
        SubjectId = subjectId,
        Deadline = deadline ?? Now.AddDays(14),
        MaxMarks = maxMarks
    };

    /// <summary>
    /// A full-representation update built from the stored assignment, with only the
    /// named fields changed. Mirrors what an edit form posts, so a test that changes
    /// nothing else is not accidentally also testing an immutability rule.
    /// </summary>
    private static UpdateAssignmentRequest UpdateRequest(
        Assignment assignment,
        string? title = null,
        Guid? classRoomId = null,
        Guid? subjectId = null,
        DateTime? deadline = null,
        int? maxMarks = null) => new()
    {
        Title = title ?? assignment.Title,
        Description = assignment.Description,
        ClassRoomId = classRoomId ?? assignment.ClassRoomId,
        SubjectId = subjectId ?? assignment.SubjectId,
        Deadline = deadline ?? assignment.Deadline,
        MaxMarks = maxMarks ?? assignment.MaxMarks
    };

    private static Assignment Draft(
        Guid teacherId,
        Guid classRoomId,
        Guid subjectId,
        DateTime? deadline = null,
        int maxMarks = 20) =>
        Stored(AssignmentStatus.Draft, teacherId, classRoomId, subjectId, deadline, maxMarks);

    private static Assignment Published(
        Guid teacherId,
        Guid classRoomId,
        Guid subjectId,
        DateTime? deadline = null,
        int maxMarks = 20) =>
        Stored(AssignmentStatus.Published, teacherId, classRoomId, subjectId, deadline, maxMarks);

    private static Assignment Stored(
        AssignmentStatus status,
        Guid teacherId,
        Guid classRoomId,
        Guid subjectId,
        DateTime? deadline,
        int maxMarks) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Kinematics Problem Set",
        Description = "Solve the five problems set in class.",
        ClassRoomId = classRoomId,
        SubjectId = subjectId,
        CreatedByTeacherId = teacherId,
        Deadline = deadline ?? Now.AddDays(14),
        MaxMarks = maxMarks,
        Status = status,
        CreatedAt = Now.AddDays(-5),
        UpdatedAt = Now.AddDays(-5)
    };
}
