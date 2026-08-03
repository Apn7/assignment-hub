using System.Linq.Expressions;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;

namespace AssignmentHub.Application.Common;

/// <summary>
/// Reusable predicates over <see cref="Assignment"/>.
/// </summary>
public static class AssignmentQueries
{
    /// <summary>
    /// The one and only definition of "a student in this class may see this
    /// assignment": published, and targeted at their class.
    /// </summary>
    /// <remarks>
    /// Deliberately an <see cref="Expression"/> rather than a
    /// <see cref="Func{T, TResult}"/>. The repository hands it to EF Core, which
    /// translates it into the SQL <c>WHERE</c> clause, so drafts and other
    /// classes are excluded by the database rather than filtered out afterwards.
    /// The unit tests compile the same expression and assert on it, which means
    /// the rule that is tested is literally the rule that runs.
    ///
    /// Both the list and the detail query go through this, so the two endpoints
    /// cannot disagree about what a student is allowed to see.
    /// </remarks>
    public static Expression<Func<Assignment, bool>> VisibleToStudent(Guid classRoomId) =>
        assignment => assignment.Status == AssignmentStatus.Published
                      && assignment.ClassRoomId == classRoomId;
}
