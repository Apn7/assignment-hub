namespace AssignmentHub.Tests.TestDoubles;

/// <summary>
/// Frozen, movable clock. Every rule that compares against "now" — token expiry,
/// the future-deadline check at publish time — is only testable exactly rather
/// than approximately because the clock is injected.
/// </summary>
internal sealed class FakeTimeProvider : TimeProvider
{
    public FakeTimeProvider(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    /// <summary>Settable so a test can step the clock forward mid-scenario.</summary>
    public DateTimeOffset UtcNow { get; set; }

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
