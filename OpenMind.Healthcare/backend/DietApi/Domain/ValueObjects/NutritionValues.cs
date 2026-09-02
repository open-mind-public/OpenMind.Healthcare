using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

/// <summary>
/// What a food contributes: calories plus the three macronutrients.
/// </summary>
/// <remarks>
/// Calories are whole kilocalories, deliberately. EF Core maps <c>decimal</c> to SQLite
/// <c>TEXT</c>, which cannot be summed or averaged numerically, and the average daily intake
/// statistic needs exactly that. Nutrition labels are whole kilocalories anyway, so nothing is
/// lost. Macronutrient grams stay decimal because they are only ever totalled inside a single
/// day, in memory, and never aggregated in SQL.
/// </remarks>
public class NutritionValues : ValueObject
{
    public int Calories { get; private set; }
    public decimal ProteinG { get; private set; }
    public decimal CarbsG { get; private set; }
    public decimal FatG { get; private set; }

    // Private parameterless constructor for EF Core
    private NutritionValues() { }

    private NutritionValues(int calories, decimal proteinG, decimal carbsG, decimal fatG)
    {
        Calories = calories;
        ProteinG = Math.Round(proteinG, 1);
        CarbsG = Math.Round(carbsG, 1);
        FatG = Math.Round(fatG, 1);
    }

    public static NutritionValues Create(int calories, decimal proteinG = 0m, decimal carbsG = 0m, decimal fatG = 0m)
    {
        if (calories < 0)
            throw new DomainException("Calories cannot be negative");

        if (proteinG < 0 || carbsG < 0 || fatG < 0)
            throw new DomainException("Macronutrient grams cannot be negative");

        return new NutritionValues(calories, proteinG, carbsG, fatG);
    }

    public static NutritionValues Zero() => new(0, 0m, 0m, 0m);

    public NutritionValues Plus(NutritionValues other) =>
        new(Calories + other.Calories,
            ProteinG + other.ProteinG,
            CarbsG + other.CarbsG,
            FatG + other.FatG);

    /// <summary>
    /// Scales a serving's values by how many servings were eaten. Fractional quantities are
    /// supported; calories round away from zero so half a serving of a 45 kcal food reads 23,
    /// not 22.
    /// </summary>
    public NutritionValues Times(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        return new NutritionValues(
            (int)Math.Round(Calories * quantity, MidpointRounding.AwayFromZero),
            ProteinG * quantity,
            CarbsG * quantity,
            FatG * quantity);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Calories;
        yield return ProteinG;
        yield return CarbsG;
        yield return FatG;
    }
}
