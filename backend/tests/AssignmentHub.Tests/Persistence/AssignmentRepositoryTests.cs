using AssignmentHub.Application.DTOs.Assignments;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;
using AssignmentHub.Infrastructure.Persistence;
using AssignmentHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AssignmentHub.Tests.Persistence;

/// <summary>
/// The repository's query composition, exercised through a real
/// <see cref="AppDbContext"/> on the in-memory provider.
/// </summary>
/// <remarks>
/// These cover what a service test with a fake store cannot: that the filters,
/// ordering, scoping and <c>Include</c>s are actually written into the queries the
/// service relies on. Worth stating the limit — the in-memory provider runs LINQ
/// against objects, so it proves the query is composed correctly but not that
/// Npgsql translates it. That last step is covered by the manual Swagger run
/// against Postgres recorded in docs/assignments.md.
/// </remarks>
public class AssignmentRepositoryTests
{
    private static readonly DateTime Now = new(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid Teacher1 = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid Teacher2 = new("20000000-0000-0000-0000-000000000002");
    private static readonly Guid Class9A = new("40000000-0000-0000-0000-000000000001");
    private static readonly Guid Class10A = new("40000000-0000-0000-0000-000000000002");
    private static readonly Guid Physics = new("50000000-0000-0000-0000-000000000001");
    private static readonly Guid English = new("50000000-0000-0000-0000-000000000003");

    private static readonly Guid PublishedFor9A = new("60000000-0000-0000-0000-000000000001");
    private static readonly Guid DraftFor9A = new("60000000-0000-0000-0000-000000000002");
    private static readonly Guid PublishedFor10A = new("60000000-0000-0000-0000-000000000003");
    private static readonly Guid SecondPublishedFor9A = new("60000000-0000-0000-0000-000000000004");

    [Fact]
    public async Task ListVisibleToStudentAsync_ReturnsOnlyPublishedAssignmentsForThatClass()
    {
        await using var context = await SeededContextAsync();

        var results = await new AssignmentRepository(context).ListVisibleToStudentAsync(Class9A);

        results.Select(assignment => assignment.Id)
            .Should().BeEquivalentTo(new[] { PublishedFor9A, SecondPublishedFor9A });
    }

    [Fact]
    public async Task ListVisibleToStudentAsync_OrdersByNearestDeadlineFirst()
    {
        await using var context = await SeededContextAsync();

        var results = await new AssignmentRepository(context).ListVisibleToStudentAsync(Class9A);

        results.Select(assignment => assignment.Deadline).Should().BeInAscendingOrder();
    }

    [Theory]
    [InlineData(nameof(DraftFor9A))]
    [InlineData(nameof(PublishedFor10A))]
    public async Task GetVisibleToStudentAsync_ReturnsNullForAnythingOutsideTheStudentsView(string target)
    {
        await using var context = await SeededContextAsync();

        var id = target == nameof(DraftFor9A) ? DraftFor9A : PublishedFor10A;

        var result = await new AssignmentRepository(context).GetVisibleToStudentAsync(id, Class9A);

        // A draft in their own class and a published assignment for another class are
        // both simply absent, which is what lets the service answer 404 for each.
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetVisibleToStudentAsync_ReturnsAPublishedAssignmentForTheirClass()
    {
        await using var context = await SeededContextAsync();

        var result = await new AssignmentRepository(context).GetVisibleToStudentAsync(PublishedFor9A, Class9A);

        result.Should().NotBeNull();
        result!.Id.Should().Be(PublishedFor9A);
    }

    [Fact]
    public async Task ListForTeacherAsync_ReturnsOnlyThatTeachersWorkInBothStatuses()
    {
        await using var context = await SeededContextAsync();

        var results = await new AssignmentRepository(context)
            .ListForTeacherAsync(Teacher1, AssignmentFilter.None);

        results.Select(assignment => assignment.Id)
            .Should().BeEquivalentTo(new[] { PublishedFor9A, DraftFor9A, SecondPublishedFor9A });
    }

    [Fact]
    public async Task ListForTeacherAsync_NarrowsByEveryFilterDimension()
    {
        await using var context = await SeededContextAsync();

        var results = await new AssignmentRepository(context).ListForTeacherAsync(
            Teacher1,
            new AssignmentFilter
            {
                ClassRoomId = Class9A,
                SubjectId = Physics,
                Status = AssignmentStatus.Draft
            });

        results.Select(assignment => assignment.Id).Should().Equal(DraftFor9A);
    }

    [Fact]
    public async Task ListAllAsync_SeesEveryTeachersWork()
    {
        await using var context = await SeededContextAsync();

        var results = await new AssignmentRepository(context).ListAllAsync(AssignmentFilter.None);

        results.Should().HaveCount(4);
        results.Select(assignment => assignment.CreatedByTeacherId).Distinct()
            .Should().BeEquivalentTo(new[] { Teacher1, Teacher2 });
    }

    [Fact]
    public async Task ListAllAsync_NarrowsByStatusAcrossTeachers()
    {
        await using var context = await SeededContextAsync();

        var results = await new AssignmentRepository(context)
            .ListAllAsync(new AssignmentFilter { Status = AssignmentStatus.Published });

        results.Select(assignment => assignment.Id)
            .Should().BeEquivalentTo(new[] { PublishedFor9A, SecondPublishedFor9A, PublishedFor10A });
    }

    [Fact]
    public async Task GetDetailAsync_LoadsTheClassSubjectAndTeacherNames()
    {
        await using var context = await SeededContextAsync();

        var assignment = await new AssignmentRepository(context).GetDetailAsync(PublishedFor9A);

        // AssignmentResponse reads these three navigations. If an Include went
        // missing, every list in the UI would silently render blank names.
        var response = AssignmentResponse.FromAssignment(assignment!);
        response.ClassRoomName.Should().Be("Class 9 - A");
        response.SubjectName.Should().Be("Physics");
        response.CreatedByTeacherName.Should().Be("Ayesha Rahman");
    }

    [Fact]
    public async Task GetForUpdateAsync_ReturnsATrackedEntitySoWritesPersist()
    {
        await using var context = await SeededContextAsync();
        var repository = new AssignmentRepository(context);

        var assignment = await repository.GetForUpdateAsync(DraftFor9A);
        assignment!.Status = AssignmentStatus.Published;
        await repository.SaveChangesAsync();

        // Proves the "for update" query really is tracked: the service mutates the
        // entity and never calls an Update method.
        context.ChangeTracker.Clear();
        var reloaded = await repository.GetForUpdateAsync(DraftFor9A);
        reloaded!.Status.Should().Be(AssignmentStatus.Published);
    }

    private static async Task<AppDbContext> SeededContextAsync()
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            // A fresh store per test, so ordering and mutation tests cannot bleed.
            .UseInMemoryDatabase($"assignments-{Guid.NewGuid()}")
            .Options);

        context.ClassRooms.AddRange(
            new ClassRoom { Id = Class9A, Name = "Class 9 - A" },
            new ClassRoom { Id = Class10A, Name = "Class 10 - A" });

        context.Subjects.AddRange(
            new Subject { Id = Physics, Name = "Physics" },
            new Subject { Id = English, Name = "English" });

        context.Users.AddRange(
            Teacher(Teacher1, "Ayesha Rahman", "teacher1@assignmenthub.local"),
            Teacher(Teacher2, "Imran Hossain", "teacher2@assignmenthub.local"));

        context.Assignments.AddRange(
            // Deliberately inserted out of deadline order, so an ordering assertion
            // cannot pass by accident.
            Assignment(SecondPublishedFor9A, AssignmentStatus.Published, Teacher1, Class9A, Physics, Now.AddDays(20)),
            Assignment(PublishedFor9A, AssignmentStatus.Published, Teacher1, Class9A, Physics, Now.AddDays(3)),
            Assignment(DraftFor9A, AssignmentStatus.Draft, Teacher1, Class9A, Physics, Now.AddDays(14)),
            Assignment(PublishedFor10A, AssignmentStatus.Published, Teacher2, Class10A, English, Now.AddDays(5)));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return context;
    }

    private static User Teacher(Guid id, string fullName, string email) => new()
    {
        Id = id,
        FullName = fullName,
        Email = email,
        PasswordHash = "not-a-real-hash",
        Role = UserRole.Teacher,
        CreatedAt = Now.AddYears(-1)
    };

    private static Assignment Assignment(
        Guid id,
        AssignmentStatus status,
        Guid teacherId,
        Guid classRoomId,
        Guid subjectId,
        DateTime deadline) => new()
    {
        Id = id,
        Title = "Problem set",
        Description = "Work through the questions set in class.",
        ClassRoomId = classRoomId,
        SubjectId = subjectId,
        CreatedByTeacherId = teacherId,
        Deadline = deadline,
        MaxMarks = 20,
        Status = status,
        CreatedAt = Now.AddDays(-5),
        UpdatedAt = Now.AddDays(-5)
    };
}
