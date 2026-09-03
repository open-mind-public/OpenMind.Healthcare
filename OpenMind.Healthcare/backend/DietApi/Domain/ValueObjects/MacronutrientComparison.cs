using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

/// <summary>
/// What a member actually ate, against what they were aiming for.
/// </summary>
/// <remarks>
/// <para>
/// Figures are daily averages over logged days, and the target is the average of each day's
/// <em>own stored</em> target rather than the plan's current one. That is what lets a period
/// spanning a target change be judged against what was actually in force (FR-011).
/// </para>
/// <para>
/// <see cref="HasTargets"/> false means the member's plan carries a calorie target and no
/// macronutrient ones. The split is still presented; nothing is invented to compare it against
/// (FR-012).
/// </para>
/// </remarks>
public class MacronutrientComparison : ValueObject
{
    public decimal ProteinG { get; private set; }
    public decimal CarbsG { get; private set; }
    public decimal FatG { get; private set; }

    public decimal? TargetProteinG { get; private set; }
    public decimal? TargetCarbsG { get; private set; }
    public decimal? TargetFatG { get; private set; }

    /// <summary>Share of energy from each macronutrient, at 4/4/9 kcal per gram.</summary>
    public decimal ProteinShare { get; private set; }
    public decimal CarbsShare { get; private set; }
    public decimal FatShare { get; private set; }

    public int AveragedOverDays { get; private set; }

    public bool HasTargets => TargetProteinG.HasValue || TargetCarbsG.HasValue || TargetFatG.HasValue;

    private MacronutrientComparison() { }

    public const int ProteinKcalPerGram = 4;
    public const int CarbsKcalPerGram = 4;
    public const int FatKcalPerGram = 9;

    public static MacronutrientComparison Create(
        decimal proteinG,
        decimal carbsG,
        decimal fatG,
        int averagedOverDays,
        decimal? targetProteinG = null,
        decimal? targetCarbsG = null,
        decimal? targetFatG = null)
    {
        if (averagedOverDays < 0)
            throw new DomainException("Days averaged over cannot be negative");

        if (proteinG < 0 || carbsG < 0 || fatG < 0)
            throw new DomainException("Macronutrient grams cannot be negative");

        // Energy from the macronutrients themselves, not from the day's calorie total - the two
        // differ slightly, and dividing by the calorie total would give shares that do not sum to
        // 100 for reasons a member could not possibly work out.
        var energy = (proteinG * ProteinKcalPerGram) + (carbsG * CarbsKcalPerGram) + (fatG * FatKcalPerGram);

        return new MacronutrientComparison
        {
            ProteinG = Math.Round(proteinG, 1),
            CarbsG = Math.Round(carbsG, 1),
            FatG = Math.Round(fatG, 1),
            TargetProteinG = targetProteinG.HasValue ? Math.Round(targetProteinG.Value, 1) : null,
            TargetCarbsG = targetCarbsG.HasValue ? Math.Round(targetCarbsG.Value, 1) : null,
            TargetFatG = targetFatG.HasValue ? Math.Round(targetFatG.Value, 1) : null,
            ProteinShare = Share(proteinG * ProteinKcalPerGram, energy),
            CarbsShare = Share(carbsG * CarbsKcalPerGram, energy),
            FatShare = Share(fatG * FatKcalPerGram, energy),
            AveragedOverDays = averagedOverDays
        };
    }

    /// <summary>A member with a plan and nothing logged in the period.</summary>
    public static MacronutrientComparison Empty() => Create(0m, 0m, 0m, 0);

    /// <summary>
    /// Protein as a share of its target, or null when there is no protein target. Used by the
    /// observation rule, and kept here so the arithmetic has one home.
    /// </summary>
    public decimal? ProteinAttainment =>
        TargetProteinG is > 0 ? ProteinG / TargetProteinG.Value : null;

    private static decimal Share(decimal part, decimal whole) =>
        whole <= 0 ? 0m : Math.Round(part * 100m / whole, 1, MidpointRounding.AwayFromZero);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ProteinG;
        yield return CarbsG;
        yield return FatG;
        yield return TargetProteinG;
        yield return TargetCarbsG;
        yield return TargetFatG;
        yield return AveragedOverDays;
    }
}
