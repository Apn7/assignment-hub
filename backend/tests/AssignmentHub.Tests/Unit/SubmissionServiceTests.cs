using AssignmentHub.Application.Common;
using AssignmentHub.Application.DTOs.Submissions;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Application.Services;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;
using AssignmentHub.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssignmentHub.Tests.Unit;

/// <summary>
/// The seven submission business rules, one or more tests each, plus the grading
/// and reopening workflow the requirement calls for. No database, no HTTP: the
/// stores are lists and the clock is frozen and movable.
/// </summary>
public class SubmissionServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Deadline = Now.AddDays(3);

    private static readonly Guid Teacher1 = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid Teacher2 = new("20000000-0000-0000-0000-000000000002");
    private static readonly Guid Student9A = new("30000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherStudent9A = new("30000000-0000-0000-0000-000000000002");
    private static readonly Guid Student10A = new("30000000-0000-0000-0000-000000000003");
    private static readonly Guid Class9A = new("40000000-0000-0000-0000-000000000001");
    private static readonly Guid Class10A = new("40000000-0000-0000-0000-000000000002");
    private static readonly Guid Physics = new("50000000-0000-0000-0000-000000000001");

    private readonly Mock<IUserRepository> _users = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(Now));

    // -----------------------------------------------------------------------
    // Rule 1: students submit only to published assignments of their own class
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SubmitAsync_ToADraftInTheirOwnClass_IsNotFound()
    {
        var draft = NewAssignment(AssignmentStatus.Draft, Class9A);
        var assignments = new FakeAssignmentRepository(draft);
        var submissions = new FakeSubmissionRepository();
        StudentIn(Student9A, Class9A);

        var result = await CreateSut(submissions, assignments)
            .SubmitAsync(Student9A, draft.Id, Answer("My working."));

        // Not Forbidden: 403 would confirm the draft exists.
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be(NotFoundMessages.Assignment);
        submissions.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitAsync_ToAnotherClassesPublishedAssignment_IsNotFound()
    {
        var otherClass = NewAssignment(AssignmentStatus.Published, Class10A);
        var assignments = new FakeAssignmentRepository(otherClass);
        var submissions = new FakeSubmissionRepository();
        StudentIn(Student9A, Class9A);

        var result = await CreateSut(submissions, assignments)
            .SubmitAsync(Student9A, otherClass.Id, Answer("My working."));

        result.Status.Should().Be(ResultStatus.NotFound);
        submissions.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitAsync_WhenTheStudentBelongsToNoClass_IsNotFound()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var assignments = new FakeAssignmentRepository(published);
        var submissions = new FakeSubmissionRepository();

        _users.Setup(users => users.GetByIdAsync(Student9A, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Student9A, Role = UserRole.Student, ClassRoomId = null });

        var result = await CreateSut(submissions, assignments)
            .SubmitAsync(Student9A, published.Id, Answer("My working."));

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task SubmitAsync_UsesTheStoredClassNotTheRequest()
    {
        var nineA = NewAssignment(AssignmentStatus.Published, Class9A);
        var assignments = new FakeAssignmentRepository(nineA);
        var submissions = new FakeSubmissionRepository();
        StudentIn(Student10A, Class10A);

        // A Class 10-A student naming a Class 9-A assignment id gets nothing, because
        // the class is looked up per request rather than taken from the caller.
        var result = await CreateSut(submissions, assignments)
            .SubmitAsync(Student10A, nineA.Id, Answer("Not my class."));

        result.Status.Should().Be(ResultStatus.NotFound);
        _users.Verify(users => users.GetByIdAsync(Student10A, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_ToAPublishedAssignmentForTheirClass_Succeeds()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var assignments = new FakeAssignmentRepository(published);
        var submissions = new FakeSubmissionRepository();
        StudentIn(Student9A, Class9A);

        var result = await CreateSut(submissions, assignments)
            .SubmitAsync(Student9A, published.Id, Answer("  My working.  "));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(SubmissionStatus.Submitted));
        result.Value.Marks.Should().BeNull("marks appear only once a teacher has graded it");
        result.Value.Feedback.Should().BeNull();
        result.Value.ReviewedAt.Should().BeNull();

        var stored = submissions.Items.Should().ContainSingle().Subject;
        stored.AnswerText.Should().Be("My working.", "surrounding whitespace is trimmed");
        stored.StudentId.Should().Be(Student9A);
        stored.SubmittedAt.Should().Be(Now);
        stored.UpdatedAt.Should().Be(Now, "an untouched submission was last updated when it was made");
    }

    // -----------------------------------------------------------------------
    // Rule 2: the deadline, and its boundary
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SubmitAsync_AfterTheDeadline_IsRejected()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var assignments = new FakeAssignmentRepository(published);
        var submissions = new FakeSubmissionRepository();
        StudentIn(Student9A, Class9A);

        _clock.UtcNow = new DateTimeOffset(Deadline.AddSeconds(1));

        var result = await CreateSut(submissions, assignments)
            .SubmitAsync(Student9A, published.Id, Answer("Late."));

        result.Status.Should().Be(ResultStatus.Conflict);
        submissions.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitAsync_ExactlyOnTheDeadline_IsOnTime()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var assignments = new FakeAssignmentRepository(published);
        var submissions = new FakeSubmissionRepository();
        StudentIn(Student9A, Class9A);

        // The boundary is deliberately inclusive: a student who makes the deadline to
        // the instant has made it. Note this is the opposite convention from
        // publishing an assignment, which requires a deadline strictly in the future.
        _clock.UtcNow = new DateTimeOffset(Deadline);

        var result = await CreateSut(submissions, assignments)
            .SubmitAsync(Student9A, published.Id, Answer("Just in time."));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAsync_OneTickAfterTheDeadline_IsRejected()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var assignments = new FakeAssignmentRepository(published);
        var submissions = new FakeSubmissionRepository();
        StudentIn(Student9A, Class9A);

        _clock.UtcNow = new DateTimeOffset(Deadline.AddTicks(1));

        var result = await CreateSut(submissions, assignments)
            .SubmitAsync(Student9A, published.Id, Answer("A tick late."));

        result.Status.Should().Be(ResultStatus.Conflict);
    }

    [Fact]
    public async Task SubmitAsync_AfterTheDeadlineOnAnAssignmentAlreadySubmitted_ReportsTheDeadline()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var existing = NewSubmission(published, Student9A);
        var assignments = new FakeAssignmentRepository(published);
        var submissions = new FakeSubmissionRepository(existing);
        StudentIn(Student9A, Class9A);

        _clock.UtcNow = new DateTimeOffset(Deadline.AddDays(1));

        var result = await CreateSut(submissions, assignments)
            .SubmitAsync(Student9A, published.Id, Answer("Late and duplicate."));

        // Deadline is checked first, so a student who missed it entirely is told about
        // the deadline rather than about an earlier attempt.
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("deadline");
    }

    [Fact]
    public async Task UpdateOwnAsync_AfterTheDeadline_IsRejected()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var existing = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(existing);

        _clock.UtcNow = new DateTimeOffset(Deadline.AddSeconds(1));

        var result = await CreateSut(submissions).UpdateOwnAsync(
            Student9A, published.Id, Revision("Too late to change."));

        result.Status.Should().Be(ResultStatus.Conflict);
        existing.AnswerText.Should().NotBe("Too late to change.");
        submissions.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateOwnAsync_ExactlyOnTheDeadline_IsOnTime()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var existing = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(existing);

        _clock.UtcNow = new DateTimeOffset(Deadline);

        var result = await CreateSut(submissions).UpdateOwnAsync(
            Student9A, published.Id, Revision("Revised at the bell."));

        result.IsSuccess.Should().BeTrue();
        existing.AnswerText.Should().Be("Revised at the bell.");
        existing.UpdatedAt.Should().Be(Deadline);
        existing.SubmittedAt.Should().Be(Now, "revising does not rewrite when it was first handed in");
    }

    // -----------------------------------------------------------------------
    // Rule 3: one submission per student per assignment
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SubmitAsync_WhenAnAnswerAlreadyExists_IsRejected()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var existing = NewSubmission(published, Student9A);
        var assignments = new FakeAssignmentRepository(published);
        var submissions = new FakeSubmissionRepository(existing);
        StudentIn(Student9A, Class9A);

        var result = await CreateSut(submissions, assignments)
            .SubmitAsync(Student9A, published.Id, Answer("A second go."));

        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("already submitted");
        submissions.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task SubmitAsync_WhenTheUniqueIndexRejectsARace_IsAConflictNotACrash()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var assignments = new FakeAssignmentRepository(published);
        // The store is empty, so the service's own existence check passes; the index
        // then refuses the insert, exactly as it would if a concurrent request from
        // this student had committed in between.
        var submissions = new FakeSubmissionRepository { RejectNextAdd = true };
        StudentIn(Student9A, Class9A);

        var result = await CreateSut(submissions, assignments)
            .SubmitAsync(Student9A, published.Id, Answer("Double-clicked."));

        // The point of the test: a lost race must read as a duplicate, with the same
        // message as the ordinary case, and must never surface as a 500.
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("already submitted");
        submissions.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitAsync_ByADifferentStudent_IsNotADuplicate()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var theirs = NewSubmission(published, Student9A);
        var assignments = new FakeAssignmentRepository(published);
        var submissions = new FakeSubmissionRepository(theirs);
        StudentIn(OtherStudent9A, Class9A);

        var result = await CreateSut(submissions, assignments)
            .SubmitAsync(OtherStudent9A, published.Id, Answer("My own working."));

        // The rule is one per student per assignment, not one per assignment.
        result.IsSuccess.Should().BeTrue();
        submissions.Items.Should().HaveCount(2);
    }

    // -----------------------------------------------------------------------
    // Rule 4: reviewed work is frozen for the student
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateOwnAsync_OnReviewedWorkBeforeTheDeadline_IsRejected()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var reviewed = NewSubmission(published, Student9A, SubmissionStatus.Reviewed, marks: 7);
        var submissions = new FakeSubmissionRepository(reviewed);

        // Comfortably inside the deadline: it is the review, not the clock, that stops
        // this.
        var result = await CreateSut(submissions).UpdateOwnAsync(
            Student9A, published.Id, Revision("Sneaking in a fix after marking."));

        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("reopen");
        reviewed.AnswerText.Should().NotContain("Sneaking");
        submissions.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateOwnAsync_OnReviewedWorkAfterTheDeadline_ReportsTheReview()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var reviewed = NewSubmission(published, Student9A, SubmissionStatus.Reviewed, marks: 7);
        var submissions = new FakeSubmissionRepository(reviewed);

        _clock.UtcNow = new DateTimeOffset(Deadline.AddDays(1));

        var result = await CreateSut(submissions).UpdateOwnAsync(
            Student9A, published.Id, Revision("Both rules broken."));

        // Both rules apply; the review is reported because it is the one the student
        // can do something about.
        result.Error.Should().Contain("reopen");
    }

    [Fact]
    public async Task UpdateOwnAsync_WhenTheStudentHasNotSubmitted_IsNotFound()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var theirs = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(theirs);

        var result = await CreateSut(submissions).UpdateOwnAsync(
            OtherStudent9A, published.Id, Revision("Not mine to edit."));

        // A student cannot reach another student's submission at all: the owner is
        // part of the query, not a check that could be skipped.
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be(NotFoundMessages.Submission);
        theirs.AnswerText.Should().NotBe("Not mine to edit.");
    }

    // -----------------------------------------------------------------------
    // Rule 5: teachers see and grade only their own assignments' submissions
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ListForAssignmentAsync_OnAnotherTeachersAssignment_IsNotFound()
    {
        var theirs = NewAssignment(AssignmentStatus.Published, Class9A, teacherId: Teacher1);
        var assignments = new FakeAssignmentRepository(theirs);
        var submissions = new FakeSubmissionRepository(NewSubmission(theirs, Student9A));

        var result = await CreateSut(submissions, assignments)
            .ListForAssignmentAsync(Teacher2, theirs.Id);

        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be(NotFoundMessages.Assignment);
    }

    [Fact]
    public async Task GetForTeacherAsync_OnAnotherTeachersSubmission_IsNotFound()
    {
        var theirs = NewAssignment(AssignmentStatus.Published, Class9A, teacherId: Teacher1);
        var submission = NewSubmission(theirs, Student9A);
        var submissions = new FakeSubmissionRepository(submission);

        var result = await CreateSut(submissions).GetForTeacherAsync(Teacher2, submission.Id);

        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be(NotFoundMessages.Submission);
    }

    [Fact]
    public async Task GradeAsync_OnAnotherTeachersSubmission_IsNotFound()
    {
        var theirs = NewAssignment(AssignmentStatus.Published, Class9A, teacherId: Teacher1);
        var submission = NewSubmission(theirs, Student9A);
        var submissions = new FakeSubmissionRepository(submission);

        var result = await CreateSut(submissions).GradeAsync(
            Teacher2, submission.Id, new GradeSubmissionRequest { Marks = 10, Feedback = "Mine now." });

        result.Status.Should().Be(ResultStatus.NotFound);
        submission.Marks.Should().BeNull();
        submission.Status.Should().Be(SubmissionStatus.Submitted);
        submissions.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task ChangeStatusAsync_OnAnotherTeachersSubmission_IsNotFound()
    {
        var theirs = NewAssignment(AssignmentStatus.Published, Class9A, teacherId: Teacher1);
        var submission = NewSubmission(theirs, Student9A, SubmissionStatus.Reviewed, marks: 7);
        var submissions = new FakeSubmissionRepository(submission);

        var result = await CreateSut(submissions).ChangeStatusAsync(
            Teacher2, submission.Id, Status(SubmissionStatus.Submitted));

        result.Status.Should().Be(ResultStatus.NotFound);
        submission.Status.Should().Be(SubmissionStatus.Reviewed);
    }

    [Fact]
    public async Task TeacherNotFoundOutcomes_ShareOneMessagePerResource()
    {
        var theirs = NewAssignment(AssignmentStatus.Published, Class9A, teacherId: Teacher1);
        var submission = NewSubmission(theirs, Student9A);
        var submissions = new FakeSubmissionRepository(submission);
        var sut = CreateSut(submissions);

        var absent = await sut.GetForTeacherAsync(Teacher1, Guid.NewGuid());
        var notMine = await sut.GetForTeacherAsync(Teacher2, submission.Id);
        var gradeNotMine = await sut.GradeAsync(
            Teacher2, submission.Id, new GradeSubmissionRequest { Marks = 1 });

        // Three reasons, one answer. Anything else lets a teacher map out colleagues'
        // marking by comparing bodies.
        new[] { absent.Error, notMine.Error, gradeNotMine.Error }
            .Should().OnlyContain(message => message == NotFoundMessages.Submission);
    }

    // -----------------------------------------------------------------------
    // Rule 6: marks must fit the assignment
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GradeAsync_AboveMaxMarks_IsUnprocessableAndNamesTheMaximum()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A, maxMarks: 10);
        var submission = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(submission);

        var result = await CreateSut(submissions).GradeAsync(
            Teacher1, submission.Id, new GradeSubmissionRequest { Marks = 11 });

        // 422 rather than 400: the same body would be perfectly valid against an
        // assignment worth 20, so the edge could not have judged it.
        result.Status.Should().Be(ResultStatus.Unprocessable);
        result.Error.Should().Contain("10", "the teacher needs to be told the actual maximum");
        submission.Marks.Should().BeNull();
        submissions.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task GradeAsync_BelowZero_IsUnprocessable()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A, maxMarks: 10);
        var submission = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(submission);

        var result = await CreateSut(submissions).GradeAsync(
            Teacher1, submission.Id, new GradeSubmissionRequest { Marks = -1 });

        // Deliberately the same status as over-maximum. Both are "that is not a
        // possible mark for this assignment", so splitting them across 400 and 422
        // would be a distinction without a difference to the caller.
        result.Status.Should().Be(ResultStatus.Unprocessable);
        submission.Status.Should().Be(SubmissionStatus.Submitted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task GradeAsync_WithinTheAssignmentsRange_IsAccepted(int marks)
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A, maxMarks: 10);
        var submission = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(submission);

        var result = await CreateSut(submissions).GradeAsync(
            Teacher1, submission.Id, new GradeSubmissionRequest { Marks = marks });

        // Both bounds inclusive: zero is a legitimate mark, and so is full marks.
        result.IsSuccess.Should().BeTrue();
        result.Value!.Marks.Should().Be(marks);
    }

    // -----------------------------------------------------------------------
    // Grading and re-grading
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GradeAsync_RecordsMarksFeedbackAndTheReviewInstant()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A, maxMarks: 10);
        var submission = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(submission);

        var result = await CreateSut(submissions).GradeAsync(
            Teacher1,
            submission.Id,
            new GradeSubmissionRequest { Marks = 8, Feedback = "  Good, but check Q4.  " });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(SubmissionStatus.Reviewed));
        result.Value.Marks.Should().Be(8);
        result.Value.Feedback.Should().Be("Good, but check Q4.");
        result.Value.MaxMarks.Should().Be(10, "a client renders 8 / 10 without a second request");
        submission.ReviewedAt.Should().Be(Now);
    }

    [Fact]
    public async Task GradeAsync_WithNoFeedback_LeavesFeedbackNull()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A, maxMarks: 10);
        var submission = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(submission);

        var result = await CreateSut(submissions).GradeAsync(
            Teacher1, submission.Id, new GradeSubmissionRequest { Marks = 6 });

        // A mark without a comment is legitimate, so feedback is optional rather than
        // an empty string.
        result.Value!.Feedback.Should().BeNull();
    }

    [Fact]
    public async Task GradeAsync_OnAlreadyReviewedWork_RegradesAndMovesTheReviewInstant()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A, maxMarks: 10);
        var submission = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(submission);
        var sut = CreateSut(submissions);

        await sut.GradeAsync(Teacher1, submission.Id,
            new GradeSubmissionRequest { Marks = 4, Feedback = "Misread the question." });
        var firstReviewedAt = submission.ReviewedAt;

        _clock.UtcNow = new DateTimeOffset(Now.AddHours(2));

        var result = await sut.GradeAsync(Teacher1, submission.Id,
            new GradeSubmissionRequest { Marks = 8, Feedback = "On appeal: my error, not yours." });

        // Re-grading is allowed on purpose. Marking mistakes happen, and a correction
        // should not need a database edit.
        result.IsSuccess.Should().BeTrue();
        submission.Marks.Should().Be(8);
        submission.Feedback.Should().Be("On appeal: my error, not yours.");
        submission.ReviewedAt.Should().Be(Now.AddHours(2));
        submission.ReviewedAt.Should().NotBe(firstReviewedAt);
    }

    [Fact]
    public async Task GradeAsync_WithoutFeedbackOnARegrade_ClearsThePreviousComment()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A, maxMarks: 10);
        var submission = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(submission);
        var sut = CreateSut(submissions);

        await sut.GradeAsync(Teacher1, submission.Id,
            new GradeSubmissionRequest { Marks = 4, Feedback = "Retracted remark." });

        await sut.GradeAsync(Teacher1, submission.Id, new GradeSubmissionRequest { Marks = 9 });

        // A grade is the whole verdict, not a patch of it, so an omitted comment means
        // "no comment" rather than "keep the old one".
        submission.Feedback.Should().BeNull();
    }

    [Fact]
    public async Task GradeAsync_DoesNotTouchTheStudentsEditTimestamp()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A, maxMarks: 10);
        var submission = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(submission);
        var updatedAtBefore = submission.UpdatedAt;

        _clock.UtcNow = new DateTimeOffset(Now.AddHours(5));
        await CreateSut(submissions).GradeAsync(
            Teacher1, submission.Id, new GradeSubmissionRequest { Marks = 5 });

        // UpdatedAt records the student's last edit. A teacher marking the work is not
        // an edit of the answer, and ReviewedAt is where that instant belongs.
        submission.UpdatedAt.Should().Be(updatedAtBefore);
    }

    // -----------------------------------------------------------------------
    // Status changes and the reopen workflow
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ChangeStatusAsync_ReopeningReviewedWork_PreservesMarksAndFeedback()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A, maxMarks: 10);
        var submission = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(submission);
        var sut = CreateSut(submissions);

        await sut.GradeAsync(Teacher1, submission.Id,
            new GradeSubmissionRequest { Marks = 6, Feedback = "Expand your reasoning." });
        var reviewedAt = submission.ReviewedAt;

        var result = await sut.ChangeStatusAsync(
            Teacher1, submission.Id, Status(SubmissionStatus.Submitted));

        // Reopening for revision is not withdrawing the mark. The previous verdict
        // stays visible until the teacher grades again — a documented decision.
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(SubmissionStatus.Submitted));
        result.Value.Marks.Should().Be(6);
        result.Value.Feedback.Should().Be("Expand your reasoning.");
        submission.ReviewedAt.Should().Be(reviewedAt, "the work really was reviewed at that instant");
    }

    [Fact]
    public async Task ChangeStatusAsync_ToTheStatusItAlreadyHas_IsANoOpSuccess()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var submission = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(submission);

        var result = await CreateSut(submissions).ChangeStatusAsync(
            Teacher1, submission.Id, Status(SubmissionStatus.Submitted));

        // Unlike publishing an assignment, this endpoint sets state rather than firing
        // an event, so repeating it is harmless rather than a conflict.
        result.IsSuccess.Should().BeTrue();
        submission.Status.Should().Be(SubmissionStatus.Submitted);
    }

    [Fact]
    public async Task ChangeStatusAsync_DoesNotTouchTheStudentsEditTimestamp()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var submission = NewSubmission(published, Student9A, SubmissionStatus.Reviewed, marks: 5);
        var submissions = new FakeSubmissionRepository(submission);
        var updatedAtBefore = submission.UpdatedAt;

        _clock.UtcNow = new DateTimeOffset(Now.AddHours(5));
        await CreateSut(submissions).ChangeStatusAsync(
            Teacher1, submission.Id, Status(SubmissionStatus.Submitted));

        submission.UpdatedAt.Should().Be(updatedAtBefore);
    }

    [Fact]
    public async Task ReopenWorkflow_LetsTheStudentReviseAndTheTeacherRegrade()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A, maxMarks: 10);
        var submission = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(submission);
        var sut = CreateSut(submissions);

        // Graded, so the student is locked out.
        await sut.GradeAsync(Teacher1, submission.Id,
            new GradeSubmissionRequest { Marks = 3, Feedback = "Q2 and Q3 are missing." });
        (await sut.UpdateOwnAsync(Student9A, published.Id, Revision("Added Q2 and Q3.")))
            .Status.Should().Be(ResultStatus.Conflict);

        // The teacher reopens it, which is what the error message told the student to
        // ask for.
        (await sut.ChangeStatusAsync(Teacher1, submission.Id, Status(SubmissionStatus.Submitted)))
            .IsSuccess.Should().BeTrue();

        // Now the revision lands, and the re-grade replaces the verdict.
        (await sut.UpdateOwnAsync(Student9A, published.Id, Revision("Added Q2 and Q3.")))
            .IsSuccess.Should().BeTrue();
        submission.AnswerText.Should().Be("Added Q2 and Q3.");

        var regraded = await sut.GradeAsync(Teacher1, submission.Id,
            new GradeSubmissionRequest { Marks = 9, Feedback = "Much better." });

        regraded.Value!.Marks.Should().Be(9);
        regraded.Value.Status.Should().Be(nameof(SubmissionStatus.Reviewed));
    }

    // -----------------------------------------------------------------------
    // Reading
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetOwnAsync_ShowsStatusMarksAndFeedbackOnceGraded()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A, maxMarks: 10);
        var submission = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(submission);
        var sut = CreateSut(submissions);

        await sut.GradeAsync(Teacher1, submission.Id,
            new GradeSubmissionRequest { Marks = 8, Feedback = "Well argued." });

        var result = await sut.GetOwnAsync(Student9A, published.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(SubmissionStatus.Reviewed));
        result.Value.Marks.Should().Be(8);
        result.Value.Feedback.Should().Be("Well argued.");
        result.Value.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOwnAsync_WhenTheStudentHasNotSubmitted_IsNotFound()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var submissions = new FakeSubmissionRepository();

        var result = await CreateSut(submissions).GetOwnAsync(Student9A, published.Id);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task GetOwnAsync_CannotReachAnotherStudentsSubmission()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A);
        var theirs = NewSubmission(published, Student9A);
        var submissions = new FakeSubmissionRepository(theirs);

        var result = await CreateSut(submissions).GetOwnAsync(OtherStudent9A, published.Id);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task ListForAssignmentAsync_OnTheirOwnAssignment_ReturnsEveryStudentsWork()
    {
        var published = NewAssignment(AssignmentStatus.Published, Class9A, teacherId: Teacher1, maxMarks: 10);
        var later = NewSubmission(published, Student9A, submittedAt: Now.AddHours(2));
        var earlier = NewSubmission(published, OtherStudent9A, submittedAt: Now);
        var assignments = new FakeAssignmentRepository(published);
        var submissions = new FakeSubmissionRepository(later, earlier);

        var result = await CreateSut(submissions, assignments)
            .ListForAssignmentAsync(Teacher1, published.Id);

        result.IsSuccess.Should().BeTrue();

        var items = result.Value!;
        // Earliest first: a teacher marks in the order the work arrived.
        items.Select(item => item.Id).Should().Equal(earlier.Id, later.Id);
        items.Select(item => item.MaxMarks).Should().AllBeEquivalentTo(10);
    }

    [Fact]
    public void SubmissionListItem_OmitsTheAnswerAndFeedback()
    {
        // A marking overview of thirty students must not carry thirty long answers.
        // The full text is one request away at GET /api/submissions/{id}.
        var properties = typeof(SubmissionListItem).GetProperties().Select(property => property.Name);

        properties.Should().NotContain(nameof(Submission.AnswerText));
        properties.Should().NotContain(nameof(Submission.Feedback));
    }

    [Fact]
    public async Task ListAllAsync_SeesEveryAssignmentsSubmissions()
    {
        var nineA = NewAssignment(AssignmentStatus.Published, Class9A, teacherId: Teacher1);
        var tenA = NewAssignment(AssignmentStatus.Published, Class10A, teacherId: Teacher2);
        var first = NewSubmission(nineA, Student9A);
        var second = NewSubmission(tenA, Student10A);
        var submissions = new FakeSubmissionRepository(first, second);

        var result = await CreateSut(submissions).ListAllAsync(SubmissionFilter.None);

        result.Value!.Select(item => item.Id).Should().BeEquivalentTo(new[] { first.Id, second.Id });
    }

    [Fact]
    public async Task ListAllAsync_NarrowsByAssignmentClassAndStatus()
    {
        var nineA = NewAssignment(AssignmentStatus.Published, Class9A, teacherId: Teacher1);
        var tenA = NewAssignment(AssignmentStatus.Published, Class10A, teacherId: Teacher2);
        var reviewedInNineA = NewSubmission(nineA, Student9A, SubmissionStatus.Reviewed, marks: 5);
        var submittedInNineA = NewSubmission(nineA, OtherStudent9A);
        var inTenA = NewSubmission(tenA, Student10A);
        var submissions = new FakeSubmissionRepository(reviewedInNineA, submittedInNineA, inTenA);
        var sut = CreateSut(submissions);

        (await sut.ListAllAsync(new SubmissionFilter { AssignmentId = nineA.Id }))
            .Value!.Select(item => item.Id)
            .Should().BeEquivalentTo(new[] { reviewedInNineA.Id, submittedInNineA.Id });

        (await sut.ListAllAsync(new SubmissionFilter { ClassRoomId = Class10A }))
            .Value!.Select(item => item.Id).Should().Equal(inTenA.Id);

        (await sut.ListAllAsync(new SubmissionFilter { Status = SubmissionStatus.Reviewed }))
            .Value!.Select(item => item.Id).Should().Equal(reviewedInNineA.Id);
    }

    // -----------------------------------------------------------------------
    // Fixtures
    // -----------------------------------------------------------------------

    private SubmissionService CreateSut(
        FakeSubmissionRepository submissions,
        FakeAssignmentRepository? assignments = null) => new(
        submissions,
        assignments ?? new FakeAssignmentRepository(),
        _users.Object,
        _clock,
        NullLogger<SubmissionService>.Instance);

    private void StudentIn(Guid studentId, Guid classRoomId) =>
        _users
            .Setup(users => users.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = studentId,
                Role = UserRole.Student,
                ClassRoomId = classRoomId
            });

    private static SubmitAnswerRequest Answer(string text) => new() { AnswerText = text };

    private static UpdateSubmissionRequest Revision(string text) => new() { AnswerText = text };

    private static ChangeSubmissionStatusRequest Status(SubmissionStatus status) => new() { Status = status };

    private static Assignment NewAssignment(
        AssignmentStatus status,
        Guid classRoomId,
        Guid? teacherId = null,
        int maxMarks = 20) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Kinematics Problem Set",
        Description = "Solve the five problems set in class.",
        ClassRoomId = classRoomId,
        SubjectId = Physics,
        CreatedByTeacherId = teacherId ?? Teacher1,
        Deadline = Deadline,
        MaxMarks = maxMarks,
        Status = status,
        CreatedAt = Now.AddDays(-5),
        UpdatedAt = Now.AddDays(-5)
    };

    /// <summary>
    /// A stored submission with its parent assignment attached, because every write
    /// rule reads the deadline, the maximum marks or the owning teacher through it.
    /// </summary>
    private static Submission NewSubmission(
        Assignment assignment,
        Guid studentId,
        SubmissionStatus status = SubmissionStatus.Submitted,
        int? marks = null,
        DateTime? submittedAt = null) => new()
    {
        Id = Guid.NewGuid(),
        AssignmentId = assignment.Id,
        Assignment = assignment,
        StudentId = studentId,
        AnswerText = "My original working.",
        SubmittedAt = submittedAt ?? Now,
        UpdatedAt = submittedAt ?? Now,
        Status = status,
        Marks = marks,
        Feedback = marks is null ? null : "Earlier remark.",
        ReviewedAt = marks is null ? null : Now.AddMinutes(-30)
    };
}
