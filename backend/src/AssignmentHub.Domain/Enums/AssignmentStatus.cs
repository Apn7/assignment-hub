namespace AssignmentHub.Domain.Enums;

/// <summary>
/// Publication state of an assignment. Students must never see
/// <see cref="Draft"/> assignments.
/// </summary>
public enum AssignmentStatus
{
    Draft = 1,
    Published = 2
}
