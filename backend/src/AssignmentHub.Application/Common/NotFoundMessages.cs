namespace AssignmentHub.Application.Common;

/// <summary>
/// The single message each resource returns when it is absent, invisible or not
/// the caller's.
/// </summary>
/// <remarks>
/// Shared rather than declared per service because two services answer for the
/// same resource: a student probing a draft goes through <c>SubmissionService</c>
/// while a teacher probing a colleague's assignment goes through
/// <c>AssignmentService</c>, and if those two replies differed the pair of them
/// would become an oracle for what exists. One constant makes them identical by
/// construction.
///
/// The reasons are still distinguished in the server log, where "not yours" is a
/// warning and "does not exist" is information.
/// </remarks>
public static class NotFoundMessages
{
    public const string Assignment = "Assignment not found.";

    public const string Submission = "Submission not found.";
}
