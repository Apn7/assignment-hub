using System.Net;
using System.Net.Http.Json;
using AssignmentHub.Application.DTOs.Assignments;
using AssignmentHub.Application.DTOs.Submissions;

namespace AssignmentHub.Tests.Integration;

/// <summary>
/// The submission workflow driven end to end over HTTP, by the three roles in turn.
/// </summary>
/// <remarks>
/// The service tests already cover each rule in isolation against a fake store. What
/// they cannot show is that the rules compose into a workflow a real client can
/// actually complete: that the id a teacher gets back from a create is the id a student
/// can submit against, that publishing genuinely changes what the student feed returns,
/// and that a grade written on one route is readable on a different one.
///
/// Every test here creates the work it operates on, so nothing depends on the fixture
/// rows staying untouched, and assertions on the student feed are written as
/// contains/does-not-contain rather than as exact sets — other tests in this class add
/// assignments to the same class.
/// </remarks>
public sealed class SubmissionWorkflowTests : IClassFixture<ApiFactory>
{
    private const int MaxMarks = 50;

    private readonly ApiFactory _factory;

    public SubmissionWorkflowTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheFullLifecycle_FromDraftThroughGradingToReopenedRevision()
    {
        var teacher = await _factory.ClientForAsync(TestData.Teacher1Email);
        var student = await _factory.ClientForAsync(TestData.Student1Email);

        // --- The teacher authors a draft ------------------------------------
        var created = await CreateDraftAsync(teacher, "Energy and Work Problem Set");

        created.Status.Should().Be("Draft", "creation must never publish");

        // --- Which the class cannot see yet ---------------------------------
        var feedBeforePublish = await student.GetFromJsonAsync<List<AssignmentResponse>>("/api/assignments");
        feedBeforePublish!.Select(a => a.Id).Should().NotContain(created.Id);

        // The detail route agrees with the feed, rather than being a second opinion.
        var detailBeforePublish = await student.GetAsync($"/api/assignments/{created.Id}");
        detailBeforePublish.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // --- The teacher publishes it ---------------------------------------
        var publishResponse = await teacher.PostAsync($"/api/assignments/{created.Id}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var published = await publishResponse.Content.ReadFromJsonAsync<AssignmentResponse>();
        published!.Status.Should().Be("Published");

        // --- And now the class can see it -----------------------------------
        var feedAfterPublish = await student.GetFromJsonAsync<List<AssignmentResponse>>("/api/assignments");
        feedAfterPublish!.Select(a => a.Id).Should().Contain(created.Id);

        // --- The student answers --------------------------------------------
        var submitResponse = await student.PostAsJsonAsync(
            $"/api/assignments/{created.Id}/submissions",
            new { answerText = "First attempt: W = F·d, so 120 J." });

        submitResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var submission = await submitResponse.Content.ReadFromJsonAsync<SubmissionResponse>();
        submission!.Status.Should().Be("Submitted");
        submission.Marks.Should().BeNull("nothing is graded on arrival");

        // --- Revises before the deadline ------------------------------------
        var reviseResponse = await student.PutAsJsonAsync(
            $"/api/assignments/{created.Id}/submissions/mine",
            new { answerText = "Second attempt: W = F·d = 40 N × 3 m = 120 J." });

        reviseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var revised = await reviseResponse.Content.ReadFromJsonAsync<SubmissionResponse>();
        revised!.AnswerText.Should().Contain("40 N × 3 m");
        revised.Id.Should().Be(submission.Id, "revising must not create a second submission");

        // --- A second submission is refused ---------------------------------
        var duplicateResponse = await student.PostAsJsonAsync(
            $"/api/assignments/{created.Id}/submissions",
            new { answerText = "Third attempt, submitted rather than updated." });

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // --- The owning teacher sees it on their marking list ---------------
        var markingList = await teacher.GetFromJsonAsync<List<SubmissionListItem>>(
            $"/api/assignments/{created.Id}/submissions");

        markingList.Should().ContainSingle()
            .Which.StudentId.Should().Be(TestData.Student1Id);

        // --- Grades it ------------------------------------------------------
        var gradeResponse = await teacher.PostAsJsonAsync(
            $"/api/submissions/{submission.Id}/grade",
            new { marks = 45, feedback = "Correct method. State the units at each step." });

        gradeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var graded = await gradeResponse.Content.ReadFromJsonAsync<SubmissionResponse>();
        graded!.Status.Should().Be("Reviewed");
        graded.Marks.Should().Be(45);
        graded.MaxMarks.Should().Be(MaxMarks);
        graded.ReviewedAt.Should().NotBeNull();

        // --- The student reads the verdict on their own route ---------------
        var studentView = await student.GetFromJsonAsync<SubmissionResponse>(
            $"/api/assignments/{created.Id}/submissions/mine");

        studentView!.Marks.Should().Be(45);
        studentView.Feedback.Should().Contain("State the units");
        studentView.Status.Should().Be("Reviewed");

        // --- And can no longer edit it --------------------------------------
        var editAfterReview = await student.PutAsJsonAsync(
            $"/api/assignments/{created.Id}/submissions/mine",
            new { answerText = "Sneaking in a correction after marking." });

        editAfterReview.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // --- Until the teacher reopens it -----------------------------------
        var reopenResponse = await teacher.PostAsJsonAsync(
            $"/api/submissions/{submission.Id}/status",
            new { status = "Submitted" });

        reopenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reopened = await reopenResponse.Content.ReadFromJsonAsync<SubmissionResponse>();
        reopened!.Status.Should().Be("Submitted");
        // Reopening for revision is not withdrawing the mark: the previous verdict stays
        // visible while the student works.
        reopened.Marks.Should().Be(45);
        reopened.Feedback.Should().Contain("State the units");

        // --- Then the student can revise again ------------------------------
        var revisionAfterReopen = await student.PutAsJsonAsync(
            $"/api/assignments/{created.Id}/submissions/mine",
            new { answerText = "Final: W = F·d = 40 N × 3 m = 120 J, units stated throughout." });

        revisionAfterReopen.StatusCode.Should().Be(HttpStatusCode.OK);

        // --- And the teacher re-grades --------------------------------------
        var regradeResponse = await teacher.PostAsJsonAsync(
            $"/api/submissions/{submission.Id}/grade",
            new { marks = 50, feedback = "Units now correct throughout. Full marks." });

        regradeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var regraded = await regradeResponse.Content.ReadFromJsonAsync<SubmissionResponse>();
        regraded!.Marks.Should().Be(50);
        regraded.Status.Should().Be("Reviewed");
    }

    [Fact]
    public async Task GradingAboveTheAssignmentsMaximum_IsRefused()
    {
        var teacher = await _factory.ClientForAsync(TestData.Teacher1Email);
        var student = await _factory.ClientForAsync(TestData.Student1Email);

        var assignment = await PublishedAssignmentAsync(teacher, "Momentum Problem Set");

        var submitResponse = await student.PostAsJsonAsync(
            $"/api/assignments/{assignment.Id}/submissions",
            new { answerText = "p = mv." });

        var submission = await submitResponse.Content.ReadFromJsonAsync<SubmissionResponse>();

        var response = await teacher.PostAsJsonAsync(
            $"/api/submissions/{submission!.Id}/grade",
            new { marks = MaxMarks + 1, feedback = "Over the ceiling." });

        // 422, not 400: the body is perfectly valid on its own terms, and only the
        // assignment it targets knows the ceiling it breaches.
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task NegativeMarks_AreRefused()
    {
        var teacher = await _factory.ClientForAsync(TestData.Teacher1Email);
        var student = await _factory.ClientForAsync(TestData.Student1Email);

        var assignment = await PublishedAssignmentAsync(teacher, "Circular Motion Problem Set");

        var submitResponse = await student.PostAsJsonAsync(
            $"/api/assignments/{assignment.Id}/submissions",
            new { answerText = "a = v²/r." });

        var submission = await submitResponse.Content.ReadFromJsonAsync<SubmissionResponse>();

        var response = await teacher.PostAsJsonAsync(
            $"/api/submissions/{submission!.Id}/grade",
            new { marks = -1, feedback = "Below the floor." });

        // Refused either at the edge or by the service; both are correct, and pinning
        // the exact code here would make the test brittle about which layer spoke first.
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PublishingTwice_IsRefused()
    {
        var teacher = await _factory.ClientForAsync(TestData.Teacher1Email);

        var assignment = await PublishedAssignmentAsync(teacher, "Optics Problem Set");

        var response = await teacher.PostAsync($"/api/assignments/{assignment.Id}/publish", null);

        // Publishing is an event, not a state to converge on — a silent second success
        // would hide a double-submitting client.
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeletingAPublishedAssignment_IsRefused()
    {
        var teacher = await _factory.ClientForAsync(TestData.Teacher1Email);

        var assignment = await PublishedAssignmentAsync(teacher, "Thermodynamics Problem Set");

        var response = await teacher.DeleteAsync($"/api/assignments/{assignment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeletingAnOwnDraft_Succeeds_AndTheDraftIsThenGone()
    {
        var teacher = await _factory.ClientForAsync(TestData.Teacher1Email);

        var draft = await CreateDraftAsync(teacher, "Draft That Gets Withdrawn");

        var deleteResponse = await teacher.DeleteAsync($"/api/assignments/{draft.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var mine = await teacher.GetFromJsonAsync<List<AssignmentResponse>>("/api/assignments/mine");
        mine!.Select(assignment => assignment.Id).Should().NotContain(draft.Id);
    }

    [Fact]
    public async Task APublishedAssignmentsMaximumMarks_CannotBeChanged()
    {
        var teacher = await _factory.ClientForAsync(TestData.Teacher1Email);

        var assignment = await PublishedAssignmentAsync(teacher, "Waves Problem Set");

        var response = await teacher.PutAsJsonAsync(
            $"/api/assignments/{assignment.Id}",
            new
            {
                title = assignment.Title,
                description = assignment.Description,
                classRoomId = assignment.ClassRoomId,
                subjectId = assignment.SubjectId,
                deadline = assignment.Deadline,
                maxMarks = MaxMarks + 10
            });

        // Submissions are interpreted against the maximum, so it is frozen once the
        // class has seen the work.
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task APublishedAssignmentsDeadline_CanBeExtendedButNotBroughtForward()
    {
        var teacher = await _factory.ClientForAsync(TestData.Teacher1Email);

        var assignment = await PublishedAssignmentAsync(teacher, "Electrostatics Problem Set");

        object Body(DateTime deadline) => new
        {
            title = assignment.Title,
            description = assignment.Description,
            classRoomId = assignment.ClassRoomId,
            subjectId = assignment.SubjectId,
            deadline,
            maxMarks = assignment.MaxMarks
        };

        var extended = await teacher.PutAsJsonAsync(
            $"/api/assignments/{assignment.Id}",
            Body(assignment.Deadline.AddDays(7)));

        extended.StatusCode.Should().Be(HttpStatusCode.OK);

        var broughtForward = await teacher.PutAsJsonAsync(
            $"/api/assignments/{assignment.Id}",
            Body(assignment.Deadline.AddDays(-1)));

        broughtForward.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a draft for the pair teacher1 is entitled to, and asserts the create
    /// itself succeeded so a later failure cannot be mistaken for a setup problem.
    /// </summary>
    private static async Task<AssignmentResponse> CreateDraftAsync(HttpClient teacher, string title)
    {
        var response = await teacher.PostAsJsonAsync(
            "/api/assignments",
            new
            {
                title,
                description = "Work through the questions set in class.",
                classRoomId = TestData.Class9AId,
                subjectId = TestData.PhysicsId,
                deadline = DateTime.UtcNow.AddDays(7),
                maxMarks = MaxMarks
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await Explain(response));

        return (await response.Content.ReadFromJsonAsync<AssignmentResponse>())!;
    }

    private static async Task<AssignmentResponse> PublishedAssignmentAsync(
        HttpClient teacher,
        string title)
    {
        var draft = await CreateDraftAsync(teacher, title);

        var response = await teacher.PostAsync($"/api/assignments/{draft.Id}/publish", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await Explain(response));

        return (await response.Content.ReadFromJsonAsync<AssignmentResponse>())!;
    }

    /// <summary>Puts the response body into the assertion message when setup fails.</summary>
    private static async Task<string> Explain(HttpResponseMessage response) =>
        $"the API answered {(int)response.StatusCode} with: {await response.Content.ReadAsStringAsync()}";
}
