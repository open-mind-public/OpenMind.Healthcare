using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

public record CategoryShare(FoodCategory Category, int Kilocalories, decimal ShareOfTotal);

/// <summary>
/// Where a period's energy came from, by food category.
/// </summary>
/// <remarks>
/// Exhaustive over <see cref="FoodCategory"/> for the same reason as the meal breakdown: the parts
/// must sum to the total.
/// <para>
/// Categories are read from the food library at the time of the query rather than snapshotted onto
/// each entry. This is the one figure in analytics that can change for a closed period, and it is
/// correct that it does — a category is a classification, not a number the member saw and acted
/// on, so reclassifying a food should reclassify it everywhere.
/// </para>
/// </remarks>
public class CategoryBreakdown : ValueObject
{
    public IReadOnlyList<CategoryShare> Shares { get; private set; } = [];

    public int TotalKilocalories => Shares.Sum(s => s.Kilocalories);

    private CategoryBreakdown() { }

    public static CategoryBreakdown Create(IReadOnlyList<CategoryShare> shares)
    {
        var categories = Enum.GetValues<FoodCategory>();

        if (shares.Count != categories.Length
            || shares.Select(s => s.Category).Distinct().Count() != categories.Length)
        {
            throw new DomainException("A category breakdown must cover every category exactly once");
        }

        return new CategoryBreakdown { Shares = [.. shares.OrderBy(s => s.Category)] };
    }

    /// <summary>Share of energy from fruit and vegetables together, for the plant-share rule.</summary>
    public decimal PlantShare =>
        Shares.Where(s => s.Category is FoodCategory.Fruit or FoodCategory.Vegetable)
              .Sum(s => s.ShareOfTotal);

    protected override IEnumerable<object?> GetEqualityComponents() => Shares.Cast<object?>();
}
