using AssignmentHub.Application.DTOs.Submissions;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;
using AssignmentHub.Infrastructure.Persistence;
using AssignmentHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AssignmentHub.Tests.Persistence;

/// <summary>
/// The submission repository's query composition, through a real
/// <see cref="AppDbContext"/> on the in-memory provider.
/// </summary>
/// <remarks>
/// Covers what a fake store cannot: that the class filter really does reach through
/// the parent assignment, that ownership is part of the query rather than a check
/// the caller might skip, and that the <c>Include</c>s both projections depend on
/// are present.
///
/// Two limits worth stating. The in-memory provider runs LINQ against objects, so
/// this proves composition rather than Npgsql translation; and it does not enforce
/// unique indexes, so <c>TryAddAsync</c>'s duplicate path cannot be reached here —
/// that one is covered at the service level through
/// <c>FakeSubmissionRepository.RejectNextAdd</c>, and against real Postgres by the
/// concurrent-submit check recorded in docs/submissions.md.
/// </remarks>
public class SubmissionRepositoryTests
{
    private static readonly DateTime Now = new(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid Teacher1 = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid Teacher2 = new("20000000-0000-0000-0000-000000000002");
    private static readonly Guid Student9A = new("30000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherStudent9A = new("30000000-0000-0000-0000-000000000002");
    private static readonly Guid Student10A = new("30000000-0000-0000-0000-000000000003");
    private static readonly Guid Class9A = new("40000000-0000-0000-0000-000000000001");
    private static readonly Guid Class10A = new("40000000-0000-0000-0000-000000000002");
    private static readonly Guid Physics = new("50000000-0000-0000-0000-000000000001");
    private static readonly Guid English = new("50000000-0000-0000-0000-000000000003");

    private static readonly Guid NineAAssignment = new("60000000-0000-0000-0000-000000000001");
    private static readonly Guid TenAAssignment = new("60000000-0000-0000-0000-000000000002");

    private static readonly Guid Reviewed9A = new("70000000-0000-0000-0000-000000000001");
    private static readonly Guid Submitted9A = new("70000000-0000-0000-0000-000000000002");
    private static readonly Guid Submitted10A = new("70000000-0000-0000-0000-000000000003");

    [Fact]
    public async Task ListForAssignmentAsync_ReturnsOnlyThatAssignmentsWorkEarliestFirst()
    {
        await using var context = await SeededContextAsync();

        var results = await new SubmissionRepository(context).ListForAssignmentAsync(NineAAssignment);

        results.Select(submission => submission.Id).Should().Equal(Submitted9A, Reviewed9A);
        results.Select(submission => submission.SubmittedAt).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetOwnForUpdateAsync_ScopesToTheStudentAndLoadsTheParentAssignment()
    {
        await using var context = await SeededContextAsync();
        var repository = new SubmissionRepository(context);

        var mine = await repository.GetOwnForUpdateAsync(NineAAssignment, Student9A);
        var notMine = await repository.GetOwnForUpdateAsync(NineAAssignment, Student10A);

        mine.Should().NotBeNull();
        mine!.Id.Should().Be(Reviewed9A);
        // The service reads the deadline off this navigation; without the Include every
        // student edit would throw instead of being judged.
        mine.Assignment.Should().NotBeNull();
        mine.Assignment.Deadline.Should().Be(Now.AddDays(3));

        // A student who has no submission on this assignment simply gets nothing, so
        // ownership is not a check the service could forget.
        notMine.Should().BeNull();
    }

    [Fact]
    public async Task GetForUpdateAsync_LoadsTheParentAssignmentForTheOwnershipAndMarksRules()
    {
        await using var context = await SeededContextAsync();

        var submission = await new SubmissionRepository(context).GetForUpdateAsync(Submitted10A);

        submission!.Assignment.CreatedByTeacherId.Should().Be(Teacher2);
        submission.Assignment.MaxMarks.Should().Be(25);
    }

    [Fact]
    public async Task GetDetailAsync_LoadsTheStudentAssignmentClassAndSubjectNames()
    {
        await using var context = await SeededContextAsync();

        var submission = await new SubmissionRepository(context).GetDetailAsync(Reviewed9A);

        var response = SubmissionResponse.FromSubmission(submission!);
        response.StudentName.Should().Be("Nabila Akter");
        response.AssignmentTitle.Should().Be("Kinematics Problem Set");
        response.ClassRoomName.Should().Be("Class 9 - A");
        response.SubjectName.Should().Be("Physics");
        response.MaxMarks.Should().Be(10);
    }

    [Fact]
    public async Task GetOwnDetailAsync_ScopesToTheStudent()
    {
        await using var context = await SeededContextAsync();
        var repository = new SubmissionRepository(context);

        (await repository.GetOwnDetailAsync(NineAAssignment, Student9A))!.Id.Should().Be(Reviewed9A);
        (await repository.GetOwnDetailAsync(NineAAssignment, Student10A)).Should().BeNull();
    }

    [Fact]
    public async Task ExistsForAsync_AnswersPerStudentPerAssignment()
    {
        await using var context = await SeededContextAsync();
        var repository = new SubmissionRepository(context);

        (await repository.ExistsForAsync(NineAAssignment, Student9A)).Should().BeTrue();
        // Same assignment, different student: not a duplicate.
        (await repository.ExistsForAsync(NineAAssignment, Student10A)).Should().BeFalse();
        // Same student, different assignment: also not a duplicate.
        (await repository.ExistsForAsync(TenAAssignment, Student9A)).Should().BeFalse();
    }

    [Fact]
    public async Task TryAddAsync_InsertsAndReportsSuccess()
    {
        await using var context = await SeededContextAsync();
        var repository = new SubmissionRepository(context);

        var added = await repository.TryAddAsync(new Submission
        {
            Id = Guid.NewGuid(),
            AssignmentId = TenAAssignment,
            StudentId = Student10A,
            AnswerText = "A second student's answer.",
            SubmittedAt = Now,
            UpdatedAt = Now,
            Status = SubmissionStatus.Submitted
        });

        added.Should().BeTrue();
        (await repository.ListAllAsync(SubmissionFilter.None)).Should().HaveCount(4);
    }

    [Fact]
    public async Task ListAllAsync_ReturnsEverythingMostRecentFirst()
    {
        await using var context = await SeededContextAsync();

        var results = await new SubmissionRepository(context).ListAllAsync(SubmissionFilter.None);

        results.Should().HaveCount(3);
        results.Select(submission => submission.SubmittedAt).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task ListAllAsync_NarrowsByAssignment()
    {
        await using var context = await SeededContextAsync();

        var results = await new SubmissionRepository(context)
            .ListAllAsync(new SubmissionFilter { AssignmentId = NineAAssignment });

        results.Select(submission => submission.Id)
            .Should().BeEquivalentTo(new[] { Reviewed9A, Submitted9A });
    }

    [Fact]
    public async Task ListAllAsync_NarrowsByClassThroughTheParentAssignment()
    {
        await using var context = await SeededContextAsync();

        var results = await new SubmissionRepository(context)
            .ListAllAsync(new SubmissionFilter { ClassRoomId = Class10A });

        // A submission has no class of its own, so this filter has to reach through
        // the assignment. That is exactly what a fake store cannot vouch for.
        results.Select(submission => submission.Id).Should().Equal(Submitted10A);
    }

    [Fact]
    public async Task ListAllAsync_NarrowsByStatus()
    {
        await using var context = await SeededContextAsync();

        var results = await new SubmissionRepository(context)
            .ListAllAsync(new SubmissionFilter { Status = SubmissionStatus.Reviewed });

        results.Select(submission => submission.Id).Should().Equal(Reviewed9A);
    }

    [Fact]
    public async Task ListAllAsync_CombinesEveryFilterDimension()
    {
        await using var context = await SeededContextAsync();

        var results = await new SubmissionRepository(context).ListAllAsync(new SubmissionFilter
        {
            AssignmentId = NineAAssignment,
            ClassRoomId = Class9A,
            Status = SubmissionStatus.Submitted
        });

        results.Select(submission => submission.Id).Should().Equal(Submitted9A);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsAGradeThroughTheTrackedEntity()
    {
        await using var context = await SeededContextAsync();
        var repository = new SubmissionRepository(context);

        var submission = await repository.GetForUpdateAsync(Submitted9A);
        submission!.Marks = 7;
        submission.Status = SubmissionStatus.Reviewed;
        submission.ReviewedAt = Now;
        await repository.SaveChangesAsync();

        // Proves the "for update" query is tracked: the service mutates the entity and
        // never calls an Update method.
        context.ChangeTracker.Clear();
        var reloaded = await repository.GetForUpdateAsync(Submitted9A);
        reloaded!.Marks.Should().Be(7);
        reloaded.Status.Should().Be(SubmissionStatus.Reviewed);
    }

    private static async Task<AppDbContext> SeededContextAsync()
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"submissions-{Guid.NewGuid()}")
            .Options);

        context.ClassRooms.AddRange(
            new ClassRoom { Id = Class9A, Name = "Class 9 - A" },
            new ClassRoom { Id = Class10A, Name = "Class 10 - A" });

        context.Subjects.AddRange(
            new Subject { Id = Physics, Name = "Physics" },
            new Subject { Id = English, Name = "English" });

        context.Users.AddRange(
            User(Teacher1, "Ayesha Rahman", UserRole.Teacher),
            User(Teacher2, "Imran Hossain", UserRole.Teacher),
            User(Student9A, "Nabila Akter", UserRole.Student, Class9A),
            User(OtherStudent9A, "Tanvir Ahmed", UserRole.Student, Class9A),
            User(Student10A, "Farhan Kabir", UserRole.Student, Class10A));

        context.Assignments.AddRange(
            Assignment(NineAAssignment, "Kinematics Problem Set", Teacher1, Class9A, Physics, 10),
            Assignment(TenAAssignment, "Comprehension exercise", Teacher2, Class10A, English, 25));

        context.Submissions.AddRange(
            // Deliberately inserted out of chronological order so the ordering
            // assertions cannot pass by accident.
            Submission(Reviewed9A, NineAAssignment, Student9A, Now.AddHours(-1),
                SubmissionStatus.Reviewed, marks: 8),
            Submission(Submitted9A, NineAAssignment, OtherStudent9A, Now.AddHours(-5)),
            Submission(Submitted10A, TenAAssignment, Student10A, Now.AddMinutes(-10)));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return context;
    }

    private static User User(Guid id, string fullName, UserRole role, Guid? classRoomId = null) => new()
    {
        Id = id,
        FullName = fullName,
        Email = $"{id:N}@assignmenthub.local",
        PasswordHash = "not-a-real-hash",
        Role = role,
        ClassRoomId = classRoomId,
        CreatedAt = Now.AddYears(-1)
    };

    private static Assignment Assignment(
        Guid id,
        string title,
        Guid teacherId,
        Guid classRoomId,
        Guid subjectId,
        int maxMarks) => new()
    {
        Id = id,
        Title = title,
        Description = "Work through the questions set in class.",
        ClassRoomId = classRoomId,
        SubjectId = subjectId,
        CreatedByTeacherId = teacherId,
        Deadline = Now.AddDays(3),
        MaxMarks = maxMarks,
        Status = AssignmentStatus.Published,
        CreatedAt = Now.AddDays(-5),
        UpdatedAt = Now.AddDays(-5)
    };

    private static Submission Submission(
        Guid id,
        Guid assignmentId,
        Guid studentId,
        DateTime submittedAt,
        SubmissionStatus status = SubmissionStatus.Submitted,
        int? marks = null) => new()
    {
        Id = id,
        AssignmentId = assignmentId,
        StudentId = studentId,
        AnswerText = "My working.",
        SubmittedAt = submittedAt,
        UpdatedAt = submittedAt,
        Status = status,
        Marks = marks,
        Feedback = marks is null ? null : "Solid.",
        ReviewedAt = marks is null ? null : submittedAt.AddHours(1)
    };
}
