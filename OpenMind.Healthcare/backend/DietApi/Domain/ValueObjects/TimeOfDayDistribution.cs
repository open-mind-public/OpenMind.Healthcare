using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

public record HourShare(int Hour, int Kilocalories, decimal ShareOfTotal);

/// <summary>
/// Intake across the hours of the member's own day.
/// </summary>
/// <remarks>
/// <para>
/// The hours here are local to whoever asked, rotated from UTC by the offset they sent. No member
/// timezone is stored anywhere in this application, so the offset arrives with the request rather
/// than being looked up.
/// </para>
/// <para>
/// <see cref="IsApproximate"/> is always true, and always accompanied by its reason. The programme
/// records when an entry was <em>logged</em>, which is the only time it has; a member who logs a
/// week on Sunday night will see a Sunday-night spike. Saying so is FR-015, and it is a property
/// of the type rather than of whichever screen happens to render it.
/// </para>
/// </remarks>
public class TimeOfDayDistribution : ValueObject
{
    public const string Approximation =
        "Times are when an entry was recorded, not necessarily when the food was eaten.";

    public IReadOnlyList<HourShare> Shares { get; private set; } = [];

    public int UtcOffsetMinutes { get; private set; }

    public bool IsApproximate => true;

    public string ApproximationReason => Approximation;

    private TimeOfDayDistribution() { }

    public static TimeOfDayDistribution Create(IReadOnlyList<HourShare> shares, int utcOffsetMinutes)
    {
        if (shares.Count != 24 || shares.Select(s => s.Hour).Distinct().Count() != 24)
            throw new DomainException("A time-of-day distribution must cover all 24 hours exactly once");

        return new TimeOfDayDistribution
        {
            Shares = [.. shares.OrderBy(s => s.Hour)],
            UtcOffsetMinutes = utcOffsetMinutes
        };
    }

    public int TotalKilocalories => Shares.Sum(s => s.Kilocalories);

    /// <summary>
    /// Share of the period's energy logged at or after a given local hour, for the late-eating rule.
    /// </summary>
    public decimal ShareAtOrAfter(int hour) =>
        Shares.Where(s => s.Hour >= hour).Sum(s => s.ShareOfTotal);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UtcOffsetMinutes;

        foreach (var share in Shares)
        {
            yield return share;
        }
    }
}
