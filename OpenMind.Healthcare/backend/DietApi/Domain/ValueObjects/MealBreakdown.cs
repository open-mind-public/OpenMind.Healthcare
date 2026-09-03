using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

public record MealShare(MealType Meal, int Kilocalories, decimal ShareOfTotal, int EntryCount);

/// <summary>
/// Where a period's energy came from, by meal.
/// </summary>
/// <remarks>
/// Exhaustive over <see cref="MealType"/>: a meal with nothing logged appears at zero rather than
/// being absent. That is what lets the parts always sum to the total, which is a thing a member
/// can check by adding four numbers and which SC-002 asserts (FR-006).
/// </remarks>
public class MealBreakdown : ValueObject
{
    public IReadOnlyList<MealShare> Shares { get; private set; } = [];

    public int TotalKilocalories => Shares.Sum(s => s.Kilocalories);

    private MealBreakdown() { }

    public static MealBreakdown Create(IReadOnlyList<MealShare> shares)
    {
        var meals = Enum.GetValues<MealType>();

        if (shares.Count != meals.Length || shares.Select(s => s.Meal).Distinct().Count() != meals.Length)
            throw new DomainException("A meal breakdown must cover every meal exactly once");

        return new MealBreakdown { Shares = [.. shares.OrderBy(s => s.Meal)] };
    }

    protected override IEnumerable<object?> GetEqualityComponents() => Shares.Cast<object?>();
}
