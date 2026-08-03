namespace AssignmentHub.Application.DTOs.Submissions;

/// <summary>Body of <c>POST /api/submissions/{id}/grade</c>.</summary>
public sealed class GradeSubmissionRequest
{
    /// <summary>
    /// Between zero and the assignment's <c>MaxMarks</c> inclusive.
    /// </summary>
    /// <remarks>
    /// Deliberately unvalidated at the edge. The upper bound belongs to the
    /// assignment being graded, so no request validator can know it; the service
    /// checks the range and returns 422 naming the actual maximum. Putting a
    /// half-rule here as well would mean negative marks failed with a different
    /// status code from over-maximum marks, for no reason a caller could follow.
    /// </remarks>
    public int Marks { get; init; }

    /// <summary>
    /// Optional. A teacher may record a mark without comment, so this is nullable
    /// rather than required.
    /// </summary>
    public string? Feedback { get; init; }
}
