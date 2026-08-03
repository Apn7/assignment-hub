using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AssignmentHub.Infrastructure.Persistence.Converters;

/// <summary>
/// Forces every <see cref="DateTime"/> to UTC on the way into Postgres and marks
/// it as UTC on the way back out.
/// </summary>
/// <remarks>
/// Npgsql maps <see cref="DateTime"/> to <c>timestamptz</c> and throws if handed a
/// value whose <see cref="DateTime.Kind"/> is <c>Local</c> or <c>Unspecified</c>.
/// Applying this globally means no caller has to remember, and a value that
/// round-trips is never silently shifted.
/// </remarks>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            // Unspecified is treated as already-UTC rather than local: every
            // DateTime in this system is documented as UTC, so reinterpreting it
            // against the server's timezone would move the instant.
            model => model.Kind == DateTimeKind.Utc
                ? model
                : model.Kind == DateTimeKind.Local
                    ? model.ToUniversalTime()
                    : DateTime.SpecifyKind(model, DateTimeKind.Utc),
            provider => DateTime.SpecifyKind(provider, DateTimeKind.Utc))
    {
    }
}
