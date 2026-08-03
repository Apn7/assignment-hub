namespace AssignmentHub.Domain.Entities;

/// <summary>A taught subject, e.g. Physics. Shared across classes.</summary>
public class Subject
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
