namespace DietApi.Domain.ValueObjects;

/// <summary>
/// Which meal a food entry belongs to. Used for grouping a day's entries on display.
/// </summary>
public enum MealType
{
    Breakfast,
    Lunch,
    Dinner,
    Snack
}
