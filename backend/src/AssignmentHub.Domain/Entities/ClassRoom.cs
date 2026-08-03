namespace AssignmentHub.Domain.Entities;

/// <summary>
/// A class or course group that students belong to and assignments target.
/// </summary>
/// <remarks>
/// Named ClassRoom rather than Class because <c>class</c> is a C# keyword, which
/// would force <c>@class</c> at every usage site.
/// </remarks>
public class ClassRoom
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Users with <c>UserRole.Student</c> assigned to this class.</summary>
    public ICollection<User> Students { get; set; } = new List<User>();
}
