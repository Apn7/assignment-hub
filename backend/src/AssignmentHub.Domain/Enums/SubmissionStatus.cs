namespace AssignmentHub.Domain.Enums;

/// <summary>
/// Review state of a student's submission. A teacher moves it to
/// <see cref="Reviewed"/> when marks and feedback are recorded.
/// </summary>
public enum SubmissionStatus
{
    Submitted = 1,
    Reviewed = 2
}
