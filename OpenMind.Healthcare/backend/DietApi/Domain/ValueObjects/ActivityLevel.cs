namespace DietApi.Domain.ValueObjects;

/// <summary>
/// How active a member is day to day. Multiplies their resting energy estimate.
/// </summary>
public enum ActivityLevel
{
    Sedentary,
    LightlyActive,
    ModeratelyActive,
    VeryActive,
    ExtraActive
}
