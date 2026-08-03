using AssignmentHub.Application.Common;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;

namespace AssignmentHub.Tests.Unit;

/// <summary>
/// The student visibility rule on its own, with nothing else in the way.
/// </summary>
/// <remarks>
/// <c>AssignmentQueries.VisibleToStudent</c> is the single definition used by both
/// the list query and the by-id query, so these three facts are the whole of
/// "what a student may see" — and because the same expression is handed to EF
/// Core, they are facts about the SQL as well as about the object graph.
/// </remarks>
public class AssignmentQueriesTests
{
    private static readonly Guid OwnClass = new("40000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherClass = new("40000000-0000-0000-0000-000000000002");

    [Fact]
    public void VisibleToStudent_AcceptsAPublishedAssignmentForTheirClass()
    {
        var predicate = AssignmentQueries.VisibleToStudent(OwnClass).Compile();

        predicate(Assignment(OwnClass, AssignmentStatus.Published)).Should().BeTrue();
    }

    [Fact]
    public void VisibleToStudent_RejectsADraftEvenInTheirOwnClass()
    {
        var predicate = AssignmentQueries.VisibleToStudent(OwnClass).Compile();

        predicate(Assignment(OwnClass, AssignmentStatus.Draft)).Should().BeFalse();
    }

    [Theory]
    [InlineData(AssignmentStatus.Draft)]
    [InlineData(AssignmentStatus.Published)]
    public void VisibleToStudent_RejectsAnotherClassInEveryStatus(AssignmentStatus status)
    {
        var predicate = AssignmentQueries.VisibleToStudent(OwnClass).Compile();

        predicate(Assignment(OtherClass, status)).Should().BeFalse();
    }

    private static Assignment Assignment(Guid classRoomId, AssignmentStatus status) => new()
    {
        Id = Guid.NewGuid(),
        ClassRoomId = classRoomId,
        Status = status
    };
}
