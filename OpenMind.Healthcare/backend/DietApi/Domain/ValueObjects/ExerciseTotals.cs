using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

/// <summary>
/// What a day of activity came to: time spent and energy estimated.
/// </summary>
/// <remarks>
/// Both are whole numbers, deliberately. The weekly summary aggregates them in SQL, and EF Core
/// maps <c>decimal</c> to SQLite <c>TEXT</c>, which cannot be summed numerically (ADR 0002).
/// Nothing is lost: minutes are recorded whole, and an energy estimate carries nowhere near
/// enough precision to justify a fractional kilocalorie.
/// </remarks>
public class ExerciseTotals : ValueObject
{
    public int Minutes { get; private set; }
    public int Kilocalories { get; private set; }

    // Private parameterless constructor for EF Core
    private ExerciseTotals() { }

    private ExerciseTotals(int minutes, int kilocalories)
    {
        Minutes = minutes;
        Kilocalories = kilocalories;
    }

    public static ExerciseTotals Create(int minutes, int kilocalories)
    {
        if (minutes < 0)
            throw new DomainException("Minutes cannot be negative");

        if (kilocalories < 0)
            throw new DomainException("Kilocalories cannot be negative");

        return new ExerciseTotals(minutes, kilocalories);
    }

    public static ExerciseTotals Zero() => new(0, 0);

    public ExerciseTotals Plus(ExerciseTotals other) =>
        new(Minutes + other.Minutes, Kilocalories + other.Kilocalories);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Minutes;
        yield return Kilocalories;
    }
}
