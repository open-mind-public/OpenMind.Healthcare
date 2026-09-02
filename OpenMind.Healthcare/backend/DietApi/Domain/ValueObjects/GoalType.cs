namespace DietApi.Domain.ValueObjects;

/// <summary>
/// What a member is trying to achieve. Drives the calorie adjustment applied to their
/// suggested target and the macronutrient split that goes with it.
/// </summary>
public enum GoalType
{
    LoseWeight,
    Maintain,
    GainWeight,
    EatConsistently
}
