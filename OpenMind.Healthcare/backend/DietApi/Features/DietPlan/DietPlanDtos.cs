using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;

namespace DietApi.Features.DietPlan;

public record BodyMetricsDto(decimal HeightCm, int Age, BiologicalSex Sex);

public record NutritionTargetsDto(int Calories, decimal? ProteinG, decimal? CarbsG, decimal? FatG);

public record DietPlanDto(
    Guid Id,
    GoalType Goal,
    DateOnly StartDate,
    BodyMetricsDto BodyMetrics,
    ActivityLevel ActivityLevel,
    NutritionTargetsDto Targets,
    TargetSource TargetSource,
    decimal? TargetWeightKg,
    decimal CurrentWeightKg,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record TargetSuggestionDto(
    NutritionTargetsDto SuggestedTargets,
    int RestingEnergyKcal,
    int ActivityAdjustedKcal,
    int GoalAdjustmentKcal,
    bool WasClampedToFloor,
    int FloorKcal,
    string Disclaimer);

public record SuggestTargetsRequest(
    GoalType Goal,
    BodyMetricsDto BodyMetrics,
    decimal CurrentWeightKg,
    ActivityLevel ActivityLevel);

public record CreateDietPlanRequest(
    GoalType Goal,
    DateOnly StartDate,
    BodyMetricsDto BodyMetrics,
    ActivityLevel ActivityLevel,
    decimal CurrentWeightKg,
    decimal? TargetWeightKg,
    NutritionTargetsDto Targets,
    TargetSource TargetSource);

public record UpdateDietPlanRequest(
    GoalType Goal,
    DateOnly StartDate,
    BodyMetricsDto BodyMetrics,
    ActivityLevel ActivityLevel,
    decimal? TargetWeightKg);

public record SetTargetsRequest(NutritionTargetsDto Targets, TargetSource TargetSource);

/// <summary>
/// A plan, plus a warning when the member set a target below the safe floor. The warning
/// accompanies a <em>successful</em> save - the override is allowed, not blocked.
/// </summary>
public record DietPlanResponse(DietPlanDto Plan, string? BelowSafeFloorWarning);

/// <summary>
/// An updated plan with a refreshed suggestion alongside it. <c>TargetsUnchanged</c> is always
/// true: a refreshed suggestion is offered, never applied over a member's own choice.
/// </summary>
public record UpdateDietPlanResponse(
    DietPlanDto Plan,
    TargetSuggestionDto RefreshedSuggestion,
    bool TargetsUnchanged);

public static class DietPlanMapper
{
    public static DietPlanDto ToDto(Domain.Aggregates.DietPlan plan, DateTime? asOf = null) =>
        new(plan.Id,
            plan.Goal,
            plan.StartDate,
            new BodyMetricsDto(plan.BodyMetrics.HeightCm, plan.BodyMetrics.Age, plan.BodyMetrics.Sex),
            plan.ActivityLevel,
            ToDto(plan.Targets),
            plan.TargetSource,
            plan.TargetWeightKg,
            plan.CurrentWeightKg(asOf),
            plan.CreatedAt,
            plan.UpdatedAt);

    public static NutritionTargetsDto ToDto(NutritionTargets targets) =>
        new(targets.Calories, targets.ProteinG, targets.CarbsG, targets.FatG);

    public static TargetSuggestionDto ToDto(TargetSuggestion suggestion) =>
        new(ToDto(suggestion.SuggestedTargets),
            suggestion.RestingEnergyKcal,
            suggestion.ActivityAdjustedKcal,
            suggestion.GoalAdjustmentKcal,
            suggestion.WasClampedToFloor,
            suggestion.FloorKcal,
            TargetSuggestion.Disclaimer);

    public static BodyMetrics ToDomain(BodyMetricsDto dto) =>
        BodyMetrics.Create(dto.HeightCm, dto.Age, dto.Sex);

    public static NutritionTargets ToDomain(NutritionTargetsDto dto) =>
        NutritionTargets.Create(dto.Calories, dto.ProteinG, dto.CarbsG, dto.FatG);

    /// <summary>
    /// The warning shown when a member deliberately sets a target under the recommended floor.
    /// Null when they did not, or when the target came from the system's own suggestion.
    /// </summary>
    public static string? BelowFloorWarning(NutritionTargets targets, TargetSource source, BiologicalSex sex)
    {
        if (source != TargetSource.MemberSet)
            return null;

        var floor = TargetSuggestionService.FloorFor(sex);
        return targets.Calories < floor
            ? $"{targets.Calories} calories a day is below the {floor} generally recommended for you. "
              + "Your target has been saved. Consider checking with a healthcare professional."
            : null;
    }
}
