namespace DietApi.Domain.ValueObjects;

/// <summary>
/// Input to the Mifflin-St Jeor resting energy estimate, which uses a different constant
/// for each. Also selects the safe minimum calorie floor.
/// </summary>
public enum BiologicalSex
{
    Female,
    Male
}
