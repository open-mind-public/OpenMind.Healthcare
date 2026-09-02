using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

/// <summary>
/// The daily amounts a member is aiming for. Calories are mandatory; macronutrient targets are
/// optional, so a member who only cares about calories can still use the feature.
/// </summary>
public class NutritionTargets : ValueObject
{
    public int Calories { get; private set; }
    public decimal? ProteinG { get; private set; }
    public decimal? CarbsG { get; private set; }
    public decimal? FatG { get; private set; }

    // Private parameterless constructor for EF Core
    private NutritionTargets() { }

    private NutritionTargets(int calories, decimal? proteinG, decimal? carbsG, decimal? fatG)
    {
        Calories = calories;
        ProteinG = proteinG is null ? null : Math.Round(proteinG.Value, 1);
        CarbsG = carbsG is null ? null : Math.Round(carbsG.Value, 1);
        FatG = fatG is null ? null : Math.Round(fatG.Value, 1);
    }

    public static NutritionTargets Create(int calories, decimal? proteinG = null, decimal? carbsG = null, decimal? fatG = null)
    {
        if (calories <= 0)
            throw new DomainException("Daily calorie target must be greater than zero");

        if (proteinG < 0 || carbsG < 0 || fatG < 0)
            throw new DomainException("Macronutrient targets cannot be negative");

        return new NutritionTargets(calories, proteinG, carbsG, fatG);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Calories;
        yield return ProteinG;
        yield return CarbsG;
        yield return FatG;
    }
}
