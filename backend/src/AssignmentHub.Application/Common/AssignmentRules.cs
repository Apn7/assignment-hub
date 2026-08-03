namespace AssignmentHub.Application.Common;

/// <summary>
/// Numeric limits shared by the request validators and
/// <c>AssignmentService</c>, so the 400 the caller gets from the edge and the
/// rule the service enforces can never drift apart.
/// </summary>
public static class AssignmentRules
{
    /// <summary>An assignment worth zero marks is not an assignment.</summary>
    public const int MinMaxMarks = 1;

    /// <summary>
    /// Sanity cap. Nothing in the requirements sets an upper bound; this exists so
    /// a typo like 100000 is rejected at the boundary rather than stored.
    /// </summary>
    public const int MaxAllowedMarks = 1000;

    /// <summary>Must match <c>AssignmentConfiguration</c>'s <c>HasMaxLength</c>.</summary>
    public const int TitleMaxLength = 200;
}
