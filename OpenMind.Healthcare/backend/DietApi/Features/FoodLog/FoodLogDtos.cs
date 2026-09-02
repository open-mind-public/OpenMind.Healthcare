using DietApi.Domain.Aggregates;
using DietApi.Domain.Entities;
using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietPlan;
using DietApi.Features.FoodLibrary;

namespace DietApi.Features.FoodLog;

public record FoodEntryDto(
    Guid Id,
    MealType MealType,
    string FoodName,
    string ServingLabel,
    decimal Quantity,
    NutritionValuesDto Nutrition,
    Guid FoodLibraryItemId,
    Guid ServingSizeId,
    DateTime LoggedAt);

/// <summary>
/// A day, whether or not it has been logged. <c>Version</c> is null when no day exists yet for
/// the date; otherwise it must be echoed back on any write to that day.
/// </summary>
public record LoggedDayDto(
    DateOnly Date,
    DayState State,
    Guid? Version,
    NutritionTargetsDto Targets,
    NutritionValuesDto Totals,
    int RemainingCalories,
    int OverageCalories,
    IReadOnlyList<FoodEntryDto> Entries);

/// <summary>
/// One row per day for the calendar. Days outside the plan carry <c>WithinPlan: false</c> and no
/// state - being outside the plan is a property of the range asked about, not a fourth day state.
/// </summary>
public record DaySummaryDto(
    DateOnly Date,
    bool WithinPlan,
    DayState? State,
    int? ConsumedCalories,
    int? TargetCalories);

public record DayRangeResponse(
    DateOnly From,
    DateOnly To,
    DateOnly PlanStartDate,
    IReadOnlyList<DaySummaryDto> Days);

public record AddFoodEntryRequest(
    Guid FoodLibraryItemId,
    Guid ServingSizeId,
    decimal Quantity,
    MealType MealType,
    Guid? Version);

public record UpdateFoodEntryRequest(
    Guid ServingSizeId,
    decimal Quantity,
    MealType MealType,
    Guid Version);

public static class FoodLogMapper
{
    public static FoodEntryDto ToDto(FoodEntry entry) =>
        new(entry.Id,
            entry.MealType,
            entry.FoodName,
            entry.ServingLabel,
            entry.Quantity,
            FoodLibraryMapper.ToDto(entry.Nutrition),
            entry.FoodLibraryItemId,
            entry.ServingSizeId,
            entry.LoggedAt);

    public static LoggedDayDto ToDto(LoggedDay day)
    {
        var assessment = day.Assess();

        return new LoggedDayDto(
            day.Date,
            assessment.State,
            day.Version,
            DietPlanMapper.ToDto(day.TargetSnapshot),
            FoodLibraryMapper.ToDto(day.Totals),
            assessment.RemainingCalories,
            assessment.OverageCalories,
            [.. day.Entries.OrderBy(e => e.MealType).ThenBy(e => e.LoggedAt).Select(ToDto)]);
    }

    /// <summary>A date the member has not logged. Not an error, and not an on-target day.</summary>
    public static LoggedDayDto EmptyDay(DateOnly date, NutritionTargets targets) =>
        new(date,
            DayState.NotLogged,
            null,
            DietPlanMapper.ToDto(targets),
            FoodLibraryMapper.ToDto(NutritionValues.Zero()),
            targets.Calories,
            0,
            []);

    public static DaySummaryDto ToDto(DaySummary summary) =>
        new(summary.Date,
            WithinPlan: true,
            DayAssessment.For(summary.Date, summary.ConsumedCalories, summary.TargetCalories, summary.HasEntries).State,
            summary.ConsumedCalories,
            summary.TargetCalories);
}
