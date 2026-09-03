using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.DietAnalytics.GetIntakeAnalysis;

/// <summary>
/// Where a member's calories went over a period.
/// </summary>
/// <remarks>
/// Returns null only when the member has no plan, so the endpoint can answer 404 and the client
/// can route to setup. A member with a plan and nothing logged gets a populated response full of
/// zeros — that is an answer, and an empty chart with an explanation beats an error (FR-024,
/// SC-005).
/// </remarks>
public record GetIntakeAnalysisQuery(PeriodPreset Preset) : IRequest<IntakeAnalysisResponse?>;

public class GetIntakeAnalysisHandler(
    IDietPlanRepository planRepository,
    IDietAnalyticsRepository analytics,
    AnalysisPeriodResolver resolver,
    IntakeAnalyser analyser,
    IUserService userService) : IRequestHandler<GetIntakeAnalysisQuery, IntakeAnalysisResponse?>
{
    /// <summary>How many contributing foods are worth listing before the tail stops being useful.</summary>
    public const int TopFoodCount = 10;

    public async Task<IntakeAnalysisResponse?> Handle(
        GetIntakeAnalysisQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var period = resolver.Resolve(request.Preset, plan.StartDate);

        var days = await analytics.GetDayRowsAsync(userId, period.From, period.To, cancellationToken);
        period = period.WithLoggedDays(days.Count);

        var previousDays = period.HasComparison
            ? await analytics.GetDayRowsAsync(
                userId, period.PreviousFrom!.Value, period.PreviousTo!.Value, cancellationToken)
            : null;

        var meals = await analytics.GetMealRowsAsync(userId, period.From, period.To, cancellationToken);
        var foods = await analytics.GetTopFoodRowsAsync(userId, period.From, period.To, TopFoodCount, cancellationToken);
        var categories = await analytics.GetCategoryRowsAsync(userId, period.From, period.To, cancellationToken);

        var summary = analyser.Summarise(days, period.TotalDays, previousDays);
        var mealBreakdown = analyser.BreakDownByMeal(meals);
        var categoryBreakdown = analyser.BreakDownByCategory(categories);
        var topFoods = analyser.TopFoods(foods, summary.TotalKilocalories);

        return new IntakeAnalysisResponse(
            DietAnalyticsMapper.ToDto(period),
            new IntakeSummaryDto(
                summary.TotalKilocalories,
                summary.AverageDailyKilocalories,
                summary.AveragedOverDays,
                summary.AveragedOver,
                summary.PreviousAverageDailyKilocalories,
                summary.OnTargetDays,
                summary.OverTargetDays,
                summary.NotLoggedDays),
            [.. mealBreakdown.Shares.Select(s =>
                new MealShareDto(s.Meal, s.Kilocalories, s.ShareOfTotal, s.EntryCount))],
            [.. topFoods.Select(f =>
                new FoodContributionDto(f.FoodLibraryItemId, f.FoodName, f.Kilocalories, f.ShareOfTotal, f.TimesLogged))],
            [.. categoryBreakdown.Shares.Select(s =>
                new CategoryShareDto(s.Category, s.Kilocalories, s.ShareOfTotal))]);
    }
}
