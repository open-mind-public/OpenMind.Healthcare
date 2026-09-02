using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Services;

/// <summary>
/// Proposes a daily calorie target and macronutrient split from a member's body details.
/// </summary>
/// <remarks>
/// Mifflin-St Jeor resting energy, multiplied by an activity factor, adjusted by goal, then
/// clamped to a safe minimum. Pure - no clock, no dependencies - so every branch is testable
/// directly.
/// <para>
/// The floor figures below are the one number in this feature with a genuine duty of care
/// attached. They are working defaults taken from commonly published guidance and are flagged
/// for review by whoever owns clinical content before release.
/// </para>
/// </remarks>
public class TargetSuggestionService
{
    /// <summary>Safe minimum daily calories for female members. Under review before release.</summary>
    public const int FemaleFloorKcal = 1200;

    /// <summary>Safe minimum daily calories for male members. Under review before release.</summary>
    public const int MaleFloorKcal = 1500;

    private const decimal ProteinKcalPerGram = 4m;
    private const decimal CarbsKcalPerGram = 4m;
    private const decimal FatKcalPerGram = 9m;

    public TargetSuggestion Suggest(
        BodyMetrics bodyMetrics,
        decimal currentWeightKg,
        ActivityLevel activityLevel,
        GoalType goal)
    {
        var resting = RestingEnergy(bodyMetrics, currentWeightKg);
        var activityAdjusted = (int)Math.Round(resting * ActivityFactor(activityLevel), MidpointRounding.AwayFromZero);
        var goalAdjustment = GoalAdjustmentKcal(goal);

        var floor = FloorFor(bodyMetrics.Sex);
        var beforeClamp = activityAdjusted + goalAdjustment;
        var wasClamped = beforeClamp < floor;
        var calories = wasClamped ? floor : beforeClamp;

        return TargetSuggestion.Create(
            SplitMacros(calories, goal),
            resting,
            activityAdjusted,
            goalAdjustment,
            wasClamped,
            floor);
    }

    public static int FloorFor(BiologicalSex sex) =>
        sex == BiologicalSex.Female ? FemaleFloorKcal : MaleFloorKcal;

    /// <summary>Mifflin-St Jeor. The only difference between the sexes is the final constant.</summary>
    private static int RestingEnergy(BodyMetrics metrics, decimal weightKg)
    {
        var baseline = (10m * weightKg) + (6.25m * metrics.HeightCm) - (5m * metrics.Age);
        var constant = metrics.Sex == BiologicalSex.Female ? -161m : 5m;

        return (int)Math.Round(baseline + constant, MidpointRounding.AwayFromZero);
    }

    private static decimal ActivityFactor(ActivityLevel level) => level switch
    {
        ActivityLevel.Sedentary => 1.2m,
        ActivityLevel.LightlyActive => 1.375m,
        ActivityLevel.ModeratelyActive => 1.55m,
        ActivityLevel.VeryActive => 1.725m,
        ActivityLevel.ExtraActive => 1.9m,
        _ => 1.2m
    };

    /// <summary>
    /// A 500 kcal daily deficit is roughly 0.45 kg a week, inside the commonly cited safe range.
    /// The gain adjustment is smaller because surplus turns into fat faster than deficit turns
    /// into loss.
    /// </summary>
    private static int GoalAdjustmentKcal(GoalType goal) => goal switch
    {
        GoalType.LoseWeight => -500,
        GoalType.GainWeight => 400,
        _ => 0
    };

    /// <summary>
    /// Percentage splits rather than fixed grams, so the split stays coherent when a member
    /// overrides the calorie target. All four sit inside the Acceptable Macronutrient
    /// Distribution Ranges.
    /// </summary>
    private static NutritionTargets SplitMacros(int calories, GoalType goal)
    {
        var (protein, carbs, fat) = goal switch
        {
            GoalType.LoseWeight => (0.30m, 0.40m, 0.30m),
            GoalType.GainWeight => (0.25m, 0.50m, 0.25m),
            _ => (0.20m, 0.50m, 0.30m)
        };

        return NutritionTargets.Create(
            calories,
            calories * protein / ProteinKcalPerGram,
            calories * carbs / CarbsKcalPerGram,
            calories * fat / FatKcalPerGram);
    }
}
