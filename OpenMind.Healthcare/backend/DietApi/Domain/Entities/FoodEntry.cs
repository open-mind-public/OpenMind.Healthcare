using DDD.BuildingBlocks;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Entities;

/// <summary>
/// One thing eaten on a logged day. Part of the <c>LoggedDay</c> aggregate.
/// </summary>
/// <remarks>
/// The food's name, serving label and nutrition are <em>snapshotted</em> at the moment of
/// logging. The library ids are kept for provenance but never re-read to compute anything, so
/// correcting a typo in the catalogue cannot retroactively rewrite a day the member already saw
/// assessed.
/// </remarks>
public class FoodEntry : Entity
{
    public Guid LoggedDayId { get; private set; }
    public Guid FoodLibraryItemId { get; private set; }
    public Guid ServingSizeId { get; private set; }
    public string FoodName { get; private set; } = string.Empty;
    public string ServingLabel { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public MealType MealType { get; private set; }
    public NutritionValues Nutrition { get; private set; } = null!;
    public DateTime LoggedAt { get; private set; }

    // Private parameterless constructor for EF Core
    private FoodEntry() { }

    internal static FoodEntry Log(
        Guid loggedDayId,
        Guid foodLibraryItemId,
        Guid servingSizeId,
        string foodName,
        string servingLabel,
        decimal quantity,
        MealType mealType,
        NutritionValues nutritionPerServing,
        DateTime loggedAt) =>
        new()
        {
            LoggedDayId = loggedDayId,
            FoodLibraryItemId = foodLibraryItemId,
            ServingSizeId = servingSizeId,
            FoodName = foodName,
            ServingLabel = servingLabel,
            Quantity = Math.Round(quantity, 2),
            MealType = mealType,
            Nutrition = nutritionPerServing.Times(quantity),
            LoggedAt = loggedAt
        };

    /// <summary>
    /// Re-reads the serving's values and re-snapshots them. A member's own edit is a deliberate
    /// act, unlike a background correction to the library, so it picks up the current values.
    /// </summary>
    internal void Revise(
        Guid servingSizeId,
        string servingLabel,
        decimal quantity,
        MealType mealType,
        NutritionValues nutritionPerServing)
    {
        ServingSizeId = servingSizeId;
        ServingLabel = servingLabel;
        Quantity = Math.Round(quantity, 2);
        MealType = mealType;
        Nutrition = nutritionPerServing.Times(quantity);
    }
}
