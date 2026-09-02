namespace DietApi.Domain.ValueObjects;

/// <summary>
/// How a calendar day stands against its target. Exactly three members: a day falling outside
/// the plan is not a fourth state, it is a property of the range being queried, and no logged
/// day can exist outside its own plan.
/// </summary>
public enum DayState
{
    /// <summary>No entries were recorded. Not a compliant day - it interrupts a streak.</summary>
    NotLogged,
    OnTarget,
    OverTarget
}
