namespace DietApi.Domain.ValueObjects;

/// <summary>
/// Everything an observation rule is allowed to look at.
/// </summary>
/// <remarks>
/// A single composite rather than a long parameter list, so adding a rule that needs a figure
/// nobody used before does not change every existing rule's signature. Nothing here is optional
/// except the comparison window, which genuinely may not exist.
/// <para>
/// Deliberately carries no exercise data. This is the input to every sentence the programme says
/// about a member's eating, and there is no field here from which a "net calories" claim could be
/// assembled (FR-023).
/// </para>
/// </remarks>
public record AnalyticsFigures(
    AnalysisPeriod Period,
    IntakeSummary Intake,
    MealBreakdown Meals,
    CategoryBreakdown Categories,
    IReadOnlyList<FoodContribution> TopFoods,
    MacronutrientComparison Macronutrients,
    WeekdayDistribution ByWeekday,
    TimeOfDayDistribution ByHour,
    int PreviousLoggedDays);
