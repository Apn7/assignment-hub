namespace AssignmentHub.Domain.Enums;

/// <summary>
/// The three roles the system recognises. Persisted as a string so the database
/// stays readable and adding a role never renumbers the existing ones.
/// </summary>
public enum UserRole
{
    Admin = 1,
    Teacher = 2,
    Student = 3
}
