using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

/// <summary>Which days an average divided by.</summary>
public enum AveragedOver
{
    LoggedDays,
    AllDays
}

/// <summary>
/// What a member ate over a period, and how many days they logged.
/// </summary>
/// <remarks>
/// Two denominators live in this one type, deliberately. The intake average divides by
/// <em>logged</em> days, because an average that included days the member did not log would report
/// a diet they did not eat. The day-state split counts <em>all</em> days, because "how many did I
/// miss" is the question there and excluding the misses would answer it with zero.
/// <para>
/// Neither is defensible without the other, and neither is defensible without saying which is
/// which - which is why <see cref="AveragedOverDays"/> travels inside this object rather than
/// being left for a caller to remember (FR-003).
/// </para>
/// </remarks>
public class IntakeSummary : ValueObject
{
    public int TotalKilocalories { get; private set; }
    public int AverageDailyKilocalories { get; private set; }

    /// <summary>The denominator behind the average, carried with it.</summary>
    public int AveragedOverDays { get; private set; }

    public AveragedOver AveragedOver { get; private set; }

    public int? PreviousAverageDailyKilocalories { get; private set; }

    public int OnTargetDays { get; private set; }
    public int OverTargetDays { get; private set; }
    public int NotLoggedDays { get; private set; }

    private IntakeSummary() { }

    public static IntakeSummary Create(
        int totalKilocalories,
        int loggedDays,
        int totalDays,
        int onTargetDays,
        int overTargetDays,
        int? previousAverageDailyKilocalories = null)
    {
        if (loggedDays < 0 || totalDays < 0 || loggedDays > totalDays)
            throw new DomainException("Logged days must fall within the period");

        if (onTargetDays + overTargetDays > totalDays)
            throw new DomainException("More assessed days than the period holds");

        return new IntakeSummary
        {
            TotalKilocalories = totalKilocalories,
            AverageDailyKilocalories = loggedDays == 0
                ? 0
                : (int)Math.Round((decimal)totalKilocalories / loggedDays, MidpointRounding.AwayFromZero),
            AveragedOverDays = loggedDays,
            AveragedOver = AveragedOver.LoggedDays,
            PreviousAverageDailyKilocalories = previousAverageDailyKilocalories,
            OnTargetDays = onTargetDays,
            OverTargetDays = overTargetDays,

            // Whatever is left is a day the member did not log. Derived rather than passed in, so
            // the three cannot fail to add up to the period.
            NotLoggedDays = totalDays - onTargetDays - overTargetDays
        };
    }

    /// <summary>A member with a plan and nothing logged. Zeros, not an error.</summary>
    public static IntakeSummary Empty(int totalDays) => Create(0, 0, totalDays, 0, 0);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TotalKilocalories;
        yield return AverageDailyKilocalories;
        yield return AveragedOverDays;
        yield return OnTargetDays;
        yield return OverTargetDays;
        yield return NotLoggedDays;
    }
}
