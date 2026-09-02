using DietApi.Domain.Aggregates;
using DietApi.Domain.Entities;
using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.TestSupport;

/// <summary>
/// In-memory stand-in for the curated food library, with a couple of known foods tests can log.
/// </summary>
public sealed class FakeFoodLibraryRepository : IFoodLibraryRepository
{
    private readonly List<FoodLibraryItem> _items = [];

    public static FakeFoodLibraryRepository Empty() => new();

    public static FakeFoodLibraryRepository Containing(params FoodLibraryItem[] items)
    {
        var repository = new FakeFoodLibraryRepository();
        repository._items.AddRange(items);
        return repository;
    }

    /// <summary>Porridge oats at 228 kcal for one bowl, plus a 380 kcal 100 g serving.</summary>
    public static FoodLibraryItem Oats() =>
        FoodLibraryItem.Create("Porridge oats", FoodCategory.Staple,
            ServingSize.Create("1 bowl (60 g)", 60, NutritionValues.Create(228, 8.4m, 36.0m, 4.8m)),
            ServingSize.Create("100 g", 100, NutritionValues.Create(380, 14.0m, 60.0m, 8.0m)));

    /// <summary>A banana at 105 kcal.</summary>
    public static FoodLibraryItem Banana() =>
        FoodLibraryItem.Create("Banana", FoodCategory.Fruit,
            ServingSize.Create("1 medium (118 g)", 118, NutritionValues.Create(105, 1.3m, 27.0m, 0.4m)));

    /// <summary>Deliberately enormous, for exercising the single-entry calorie ceiling.</summary>
    public static FoodLibraryItem Enormous() =>
        FoodLibraryItem.Create("Catering tub of oil", FoodCategory.Staple,
            ServingSize.Create("1 tub (5 kg)", 5000, NutritionValues.Create(9500, 0m, 0m, 1000m)));

    public Task<IReadOnlyList<FoodLibraryItem>> SearchAsync(
        string query, int limit = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult<IReadOnlyList<FoodLibraryItem>>([]);

        var normalised = FoodLibraryItem.Normalise(query);

        IReadOnlyList<FoodLibraryItem> matches =
        [
            .. _items
                .Where(i => i.SearchName.Contains(normalised, StringComparison.Ordinal))
                .OrderByDescending(i => i.SearchName.StartsWith(normalised, StringComparison.Ordinal))
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Clamp(limit, 1, 20))
        ];

        return Task.FromResult(matches);
    }

    public Task<FoodLibraryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.SingleOrDefault(i => i.Id == id));
}
