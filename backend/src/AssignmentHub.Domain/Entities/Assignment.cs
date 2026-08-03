using AssignmentHub.Domain.Enums;

namespace AssignmentHub.Domain.Entities;

/// <summary>
/// A piece of work set by a teacher for one class and subject.
/// </summary>
public class Assignment
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid ClassRoomId { get; set; }

    public Guid SubjectId { get; set; }

    /// <summary>References a <see cref="User"/> with <c>UserRole.Teacher</c>.</summary>
    public Guid CreatedByTeacherId { get; set; }

    /// <summary>UTC. Submissions may only be edited before this instant.</summary>
    public DateTime Deadline { get; set; }

    /// <summary>Upper bound for <see cref="Submission.Marks"/>.</summary>
    public int MaxMarks { get; set; }

    public AssignmentStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ClassRoom ClassRoom { get; set; } = null!;

    public Subject Subject { get; set; } = null!;

    public User CreatedByTeacher { get; set; } = null!;

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
