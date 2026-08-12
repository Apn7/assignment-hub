using System.Net;
using System.Net.Http.Json;
using AssignmentHub.Application.DTOs.Assignments;
using AssignmentHub.Application.DTOs.Submissions;
using AssignmentHub.Application.DTOs.TeacherAssignments;

namespace AssignmentHub.Tests.Integration;

/// <summary>
/// Holding the right role is not the same as having standing over a particular row.
/// These are the checks that run after the role gate has already been passed.
/// </summary>
/// <remarks>
/// Every test here uses a caller whose role is correct for the endpoint, so a
/// refusal can only have come from the resource-level check. That is the distinction
/// <see cref="RoleAuthorizationTests"/> cannot draw: a teacher reaching the grading
/// endpoint is authorized in the ASP.NET sense and still must not touch a colleague's
/// submission.
///
/// The expected status is <c>404</c> almost everywhere, and deliberately so — the
/// codebase treats "absent" and "not yours" as one answer so that holding an id
/// teaches a caller nothing. These tests pin that down, because a well-meaning change
/// to <c>403</c> would read as a clearer error message while quietly turning the API
/// into an existence oracle.
/// </remarks>
public sealed class ResourceScopingTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ResourceScopingTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // -----------------------------------------------------------------------
    // Student ← class boundary and the draft boundary
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AStudent_CannotSeeADraftAssignmentForTheirOwnClass()
    {
        var student = await _factory.ClientForAsync(TestData.Student1Email);

        var response = await student.GetAsync($"/api/assignments/{TestData.DraftFor9AId}");

        // 404 rather than 403: a 403 would confirm the id names something real, which
        // is the one thing unpublished work must not reveal.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AStudent_CannotSeeAnotherClassesAssignment()
    {
        var student = await _factory.ClientForAsync(TestData.Student1Email);

        var response = await student.GetAsync($"/api/assignments/{TestData.OpenFor10AId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AStudentsFeed_ContainsOnlyPublishedWorkForTheirOwnClass()
    {
        var student = await _factory.ClientForAsync(TestData.Student1Email);

        var feed = await student.GetFromJsonAsync<List<AssignmentResponse>>("/api/assignments");

        // The published 9-A pair, and neither the draft nor 10-A's work. Asserted as an
        // exact set: "contains what it should" would pass even while leaking.
        feed!.Select(assignment => assignment.Id)
            .Should().BeEquivalentTo(new[] { TestData.OpenFor9AId, TestData.ClosedFor9AId });
    }

    [Fact]
    public async Task AStudent_CannotSubmitToADraft()
    {
        var student = await _factory.ClientForAsync(TestData.Student1Email);

        var response = await student.PostAsJsonAsync(
            $"/api/assignments/{TestData.DraftFor9AId}/submissions",
            new { answerText = "Trying to answer work that was never published." });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AStudent_CannotSubmitToAnotherClassesAssignment()
    {
        var student = await _factory.ClientForAsync(TestData.Student1Email);

        var response = await student.PostAsJsonAsync(
            $"/api/assignments/{TestData.OpenFor10AId}/submissions",
            new { answerText = "Answering another class's assignment." });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AStudent_CannotSubmitAfterTheDeadline()
    {
        var student = await _factory.ClientForAsync(TestData.Student1Email);

        var response = await student.PostAsJsonAsync(
            $"/api/assignments/{TestData.ClosedFor9AId}/submissions",
            new { answerText = "Late answer." });

        // 409, not 404: the assignment is legitimately visible to this student, so
        // there is nothing to hide — the refusal is about its state.
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AStudent_CannotReadAnotherStudentsSubmission()
    {
        var student = await _factory.ClientForAsync(TestData.Student1Email);

        // The route only ever addresses "mine", so the way to ask for someone else's is
        // to ask on an assignment they answered and you did not.
        var response = await student.GetAsync(
            $"/api/assignments/{TestData.OpenFor10AId}/submissions/mine");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // Teacher ← ownership and entitlement boundaries
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ATeacher_CannotListSubmissionsOnAColleaguesAssignment()
    {
        var teacher2 = await _factory.ClientForAsync(TestData.Teacher2Email);

        var response = await teacher2.GetAsync(
            $"/api/assignments/{TestData.OpenFor9AId}/submissions");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ATeacher_CannotReadASubmissionOnAColleaguesAssignment()
    {
        var teacher2 = await _factory.ClientForAsync(TestData.Teacher2Email);

        var response = await teacher2.GetAsync(
            $"/api/submissions/{TestData.Student1SubmissionId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ATeacher_CannotGradeASubmissionOnAColleaguesAssignment()
    {
        var teacher2 = await _factory.ClientForAsync(TestData.Teacher2Email);

        var response = await teacher2.PostAsJsonAsync(
            $"/api/submissions/{TestData.Student1SubmissionId}/grade",
            new { marks = 50, feedback = "Marked by the wrong teacher." });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ATeacher_CannotReopenASubmissionOnAColleaguesAssignment()
    {
        var teacher2 = await _factory.ClientForAsync(TestData.Teacher2Email);

        var response = await teacher2.PostAsJsonAsync(
            $"/api/submissions/{TestData.Student1SubmissionId}/status",
            new { status = "Submitted" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ATeacher_CannotEditAColleaguesAssignment()
    {
        var teacher2 = await _factory.ClientForAsync(TestData.Teacher2Email);

        var response = await teacher2.PutAsJsonAsync(
            $"/api/assignments/{TestData.OpenFor9AId}",
            new
            {
                title = "Retitled by a teacher who does not own it",
                description = "Should never be applied.",
                classRoomId = TestData.Class9AId,
                subjectId = TestData.PhysicsId,
                deadline = DateTime.UtcNow.AddDays(30),
                maxMarks = TestData.OpenFor9AMaxMarks
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ATeacher_CannotDeleteAColleaguesAssignment()
    {
        var teacher2 = await _factory.ClientForAsync(TestData.Teacher2Email);

        var response = await teacher2.DeleteAsync($"/api/assignments/{TestData.DraftFor9AId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ATeacher_CannotCreateAnAssignmentForAPairTheyDoNotTeach()
    {
        var teacher2 = await _factory.ClientForAsync(TestData.Teacher2Email);

        var response = await teacher2.PostAsJsonAsync(
            "/api/assignments",
            new
            {
                title = "Physics for a class I do not teach",
                description = "Should be refused.",
                classRoomId = TestData.Class9AId,
                subjectId = TestData.PhysicsId,
                deadline = DateTime.UtcNow.AddDays(7),
                maxMarks = 25
            });

        // 403 here, not 404. The caller named the class and subject themselves, so
        // saying "you do not teach this" reveals nothing they did not already supply.
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ATeachersOwnList_ContainsOnlyTheirOwnAssignments()
    {
        var teacher2 = await _factory.ClientForAsync(TestData.Teacher2Email);

        var mine = await teacher2.GetFromJsonAsync<List<AssignmentResponse>>("/api/assignments/mine");

        mine!.Select(assignment => assignment.Id)
            .Should().BeEquivalentTo(new[] { TestData.OpenFor10AId });
    }

    [Fact]
    public async Task ATeachersEntitlements_ListOnlyTheirOwnPairs()
    {
        var teacher1 = await _factory.ClientForAsync(TestData.Teacher1Email);

        var pairs = await teacher1.GetFromJsonAsync<List<TeacherAssignmentResponse>>(
            "/api/teacher-assignments/mine");

        pairs.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(
                new { ClassRoomId = TestData.Class9AId, SubjectId = TestData.PhysicsId },
                options => options.ExcludingMissingMembers());
    }

    // -----------------------------------------------------------------------
    // Admin ← sees everything, by design
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AnAdmin_SeesEveryAssignmentAcrossEveryClassAndStatus()
    {
        var admin = await _factory.ClientForAsync(TestData.AdminEmail);

        var all = await admin.GetFromJsonAsync<List<AssignmentResponse>>("/api/admin/assignments");

        // Drafts included, both classes included: the audit view is the one place with
        // no scoping, and that is a requirement rather than an oversight.
        all!.Select(assignment => assignment.Id).Should().BeEquivalentTo(new[]
        {
            TestData.OpenFor9AId,
            TestData.DraftFor9AId,
            TestData.ClosedFor9AId,
            TestData.OpenFor10AId
        });
    }

    [Fact]
    public async Task AnAdmin_SeesEverySubmissionAcrossEveryTeacher()
    {
        var admin = await _factory.ClientForAsync(TestData.AdminEmail);

        var all = await admin.GetFromJsonAsync<List<SubmissionListItem>>("/api/admin/submissions");

        all!.Select(submission => submission.Id).Should().BeEquivalentTo(new[]
        {
            TestData.Student1SubmissionId,
            TestData.Student2SubmissionId
        });
    }
}
