using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

/// <summary>
/// A proposed daily target, with every intermediate step exposed.
/// </summary>
/// <remarks>
/// The steps are public so the interface can explain where the number came from rather than
/// asserting it. A member who can see the resting-energy figure and the activity adjustment is
/// far likelier to trust the result - and to notice when they mistyped their height.
/// This is a suggestion, never a prescription: the member can replace it.
/// </remarks>
public class TargetSuggestion : ValueObject
{
    public const string Disclaimer =
        "A suggestion based on general guidance, not medical advice. You can set your own target.";

    public NutritionTargets SuggestedTargets { get; private set; } = null!;
    public int RestingEnergyKcal { get; private set; }
    public int ActivityAdjustedKcal { get; private set; }
    public int GoalAdjustmentKcal { get; private set; }
    public bool WasClampedToFloor { get; private set; }
    public int FloorKcal { get; private set; }

    private TargetSuggestion() { }

    public static TargetSuggestion Create(
        NutritionTargets suggestedTargets,
        int restingEnergyKcal,
        int activityAdjustedKcal,
        int goalAdjustmentKcal,
        bool wasClampedToFloor,
        int floorKcal) =>
        new()
        {
            SuggestedTargets = suggestedTargets,
            RestingEnergyKcal = restingEnergyKcal,
            ActivityAdjustedKcal = activityAdjustedKcal,
            GoalAdjustmentKcal = goalAdjustmentKcal,
            WasClampedToFloor = wasClampedToFloor,
            FloorKcal = floorKcal
        };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SuggestedTargets;
        yield return RestingEnergyKcal;
        yield return ActivityAdjustedKcal;
        yield return GoalAdjustmentKcal;
        yield return WasClampedToFloor;
        yield return FloorKcal;
    }
}
