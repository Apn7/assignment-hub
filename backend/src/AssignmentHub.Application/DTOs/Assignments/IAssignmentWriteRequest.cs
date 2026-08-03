namespace AssignmentHub.Application.DTOs.Assignments;

/// <summary>
/// The fields a teacher supplies when writing an assignment. Create and update
/// carry the same shape but mean different things, so they stay separate types;
/// this interface exists only so their shared validation rules are written once.
/// </summary>
/// <remarks>
/// Note the absence of a status field. That is the structural half of rule 6: no
/// write request can express "make this a draft again", so a published
/// assignment cannot be reverted even by a future careless caller.
/// </remarks>
public interface IAssignmentWriteRequest
{
    string Title { get; }

    string Description { get; }

    Guid ClassRoomId { get; }

    Guid SubjectId { get; }

    /// <summary>UTC. A value without a timezone marker is read as UTC.</summary>
    DateTime Deadline { get; }

    int MaxMarks { get; }
}
