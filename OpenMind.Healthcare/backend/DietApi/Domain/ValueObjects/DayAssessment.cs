using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

/// <summary>
/// How a day stands against the target that was in force when it was logged. Derived from the
/// day's own entries and its target snapshot - never stored.
/// </summary>
public class DayAssessment : ValueObject
{
    public DateOnly Date { get; private set; }
    public int ConsumedCalories { get; private set; }
    public int TargetCalories { get; private set; }

    /// <summary>Negative once the member is over target.</summary>
    public int RemainingCalories { get; private set; }

    public DayState State { get; private set; }

    /// <summary>Zero unless the day is over target.</summary>
    public int OverageCalories { get; private set; }

    private DayAssessment() { }

    public static DayAssessment For(DateOnly date, int consumedCalories, int targetCalories, bool hasEntries)
    {
        var remaining = targetCalories - consumedCalories;

        // A day with no entries is "not logged", never a compliant day. Treating an empty day as
        // on target would reward a member for logging nothing, which is the exact opposite of
        // what the streak is meant to measure.
        var state = !hasEntries
            ? DayState.NotLogged
            : consumedCalories <= targetCalories
                ? DayState.OnTarget
                : DayState.OverTarget;

        return new DayAssessment
        {
            Date = date,
            ConsumedCalories = consumedCalories,
            TargetCalories = targetCalories,
            RemainingCalories = remaining,
            State = state,
            OverageCalories = remaining < 0 ? -remaining : 0
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Date;
        yield return ConsumedCalories;
        yield return TargetCalories;
        yield return State;
    }
}
