namespace AssignmentHub.Application.Common;

/// <summary>
/// The outcome kinds a service operation can report. Each one maps to exactly
/// one HTTP status code at the controller boundary, which is what keeps the
/// status-code discipline in a single place instead of scattered across actions.
/// </summary>
public enum ResultStatus
{
    Success = 0,

    /// <summary>The request is wrong on its own terms. → 400</summary>
    ValidationFailed = 1,

    /// <summary>The caller may not act on this class/subject at all. → 403</summary>
    Forbidden = 2,

    /// <summary>Absent, or not the caller's to see. → 404</summary>
    NotFound = 3,

    /// <summary>Well-formed, but the resource's current state forbids it. → 409</summary>
    Conflict = 4,

    /// <summary>
    /// Understood and well-formed, but a value is out of range for the resource it
    /// is aimed at — awarding 12 marks out of 10. → 422
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="ValidationFailed"/>, which is reserved for requests
    /// that are wrong without reference to anything stored. The same body may be
    /// perfectly valid against a different assignment, so the edge cannot judge it.
    /// </remarks>
    Unprocessable = 5
}

/// <summary>
/// Result of an operation that returns no value.
/// </summary>
/// <remarks>
/// Services report failure as data rather than by throwing. Exceptions would
/// push the 403-versus-404 decision into a middleware far from the rule that
/// motivates it, and would make every rule test an assertion about a thrown
/// type instead of about a returned value.
/// </remarks>
public class Result
{
    protected Result(ResultStatus status, string? error)
    {
        Status = status;
        Error = error;
    }

    public ResultStatus Status { get; }

    /// <summary>Message safe to return to the caller. Null on success.</summary>
    public string? Error { get; }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result Success() => new(ResultStatus.Success, null);

    public static Result Invalid(string error) => new(ResultStatus.ValidationFailed, error);

    public static Result Forbidden(string error) => new(ResultStatus.Forbidden, error);

    public static Result NotFound(string error) => new(ResultStatus.NotFound, error);

    public static Result Conflict(string error) => new(ResultStatus.Conflict, error);

    public static Result Unprocessable(string error) => new(ResultStatus.Unprocessable, error);
}

/// <summary>Result of an operation that returns a value on success.</summary>
public sealed class Result<T> : Result
{
    private Result(ResultStatus status, string? error, T? value)
        : base(status, error)
    {
        Value = value;
    }

    /// <summary>Set only when <see cref="Result.IsSuccess"/> is true.</summary>
    public T? Value { get; }

    public static Result<T> Success(T value) => new(ResultStatus.Success, null, value);

    // `new` on purpose: without it these names would resolve to the base-class
    // factories, which return a plain Result and would not compile at the call
    // site. Hiding them keeps `Result<AssignmentResponse>.NotFound(...)` honest.
    public static new Result<T> Invalid(string error) => new(ResultStatus.ValidationFailed, error, default);

    public static new Result<T> Forbidden(string error) => new(ResultStatus.Forbidden, error, default);

    public static new Result<T> NotFound(string error) => new(ResultStatus.NotFound, error, default);

    public static new Result<T> Conflict(string error) => new(ResultStatus.Conflict, error, default);

    public static new Result<T> Unprocessable(string error) => new(ResultStatus.Unprocessable, error, default);
}
