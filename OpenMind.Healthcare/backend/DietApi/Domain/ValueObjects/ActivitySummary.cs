using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

/// <summary>
/// How active a member has been over a window, with the window before it for comparison.
/// </summary>
/// <remarks>
/// Derived, never stored. The previous window's figures are what let the summary answer the
/// question a member is actually asking - not "how much did I do" but "is this more or less than
/// usual" - without them having to remember last week themselves.
/// </remarks>
public class ActivitySummary : ValueObject
{
    /// <summary>The window every figure here is measured over.</summary>
    public int WindowDays { get; private set; }

    /// <summary>Days with at least one session. Several sessions in a day still count once.</summary>
    public int ActiveDays { get; private set; }

    public int TotalMinutes { get; private set; }
    public int TotalKilocalories { get; private set; }

    public int PreviousWindowActiveDays { get; private set; }
    public int PreviousWindowMinutes { get; private set; }

    // Private parameterless constructor for EF Core
    private ActivitySummary() { }

    private ActivitySummary(
        int windowDays,
        int activeDays,
        int totalMinutes,
        int totalKilocalories,
        int previousWindowActiveDays,
        int previousWindowMinutes)
    {
        WindowDays = windowDays;
        ActiveDays = activeDays;
        TotalMinutes = totalMinutes;
        TotalKilocalories = totalKilocalories;
        PreviousWindowActiveDays = previousWindowActiveDays;
        PreviousWindowMinutes = previousWindowMinutes;
    }

    public static ActivitySummary Create(
        int windowDays,
        int activeDays,
        int totalMinutes,
        int totalKilocalories,
        int previousWindowActiveDays,
        int previousWindowMinutes)
    {
        if (windowDays <= 0)
            throw new DomainException("A summary window must be at least a day");

        return new ActivitySummary(
            windowDays, activeDays, totalMinutes, totalKilocalories,
            previousWindowActiveDays, previousWindowMinutes);
    }

    /// <summary>A member with no activity yet. Zeros, plainly - not an error and not a blank.</summary>
    public static ActivitySummary Empty(int windowDays) => new(windowDays, 0, 0, 0, 0, 0);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return WindowDays;
        yield return ActiveDays;
        yield return TotalMinutes;
        yield return TotalKilocalories;
        yield return PreviousWindowActiveDays;
        yield return PreviousWindowMinutes;
    }
}
