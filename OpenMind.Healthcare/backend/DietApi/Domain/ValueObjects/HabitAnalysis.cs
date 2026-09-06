namespace DietApi.Domain.ValueObjects;

/// <summary>
/// What a member's beer and exercise records say across an analysis period, and how their eating
/// went on beer days compared with every other day.
/// </summary>
/// <remarks>
/// Derived, never stored. Carries no amount of beer and no calorie figure for it, and no "net"
/// number of any kind - consistent with the analytics feature's scope (FR-004, and 003).
/// </remarks>
public record HabitAnalysis(
    int InPlanDays,
    int BeerDays,
    decimal BeerDaysPerWeek,
    int ExerciseDays,
    decimal ExerciseDaysPerWeek,
    EatingOutcome OnBeerDays,
    EatingOutcome OnNonBeerDays)
{
    public static HabitAnalysis Empty { get; } =
        new(0, 0, 0m, 0, 0m, EatingOutcome.Empty, EatingOutcome.Empty);
}
