using DDD.BuildingBlocks;
using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietAnalytics.GetEatingPatterns;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.DietAnalytics.GetObservations;

/// <summary>
/// What the programme noticed in a member's own data.
/// </summary>
/// <remarks>
/// Assembles every figure the other three sections produce and hands them to the rules. Nothing
/// here decides what is interesting - that is entirely the engine's and the rules' business, which
/// is what keeps the judgement testable without a database.
/// </remarks>
public record GetObservationsQuery(PeriodPreset Preset, int UtcOffsetMinutes)
    : IRequest<ObservationsResponse?>;

public class GetObservationsHandler(
    IDietPlanRepository planRepository,
    IDietAnalyticsRepository analytics,
    AnalysisPeriodResolver resolver,
    IntakeAnalyser intakeAnalyser,
    MacronutrientAnalyser macroAnalyser,
    PatternAnalyser patternAnalyser,
    ObservationEngine engine,
    IUserService userService) : IRequestHandler<GetObservationsQuery, ObservationsResponse?>
{
    public async Task<ObservationsResponse?> Handle(
        GetObservationsQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        if (request.UtcOffsetMinutes is < GetEatingPatternsHandler.MinimumOffsetMinutes
            or > GetEatingPatternsHandler.MaximumOffsetMinutes)
        {
            throw new DomainException("That time zone offset is not a real one");
        }

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var period = resolver.Resolve(request.Preset, plan.StartDate);

        var days = await analytics.GetDayRowsAsync(userId, period.From, period.To, cancellationToken);
        period = period.WithLoggedDays(days.Count);

        var previousDays = period.HasComparison
            ? await analytics.GetDayRowsAsync(
                userId, period.PreviousFrom!.Value, period.PreviousTo!.Value, cancellationToken)
            : [];

        var meals = await analytics.GetMealRowsAsync(userId, period.From, period.To, cancellationToken);
        var categories = await analytics.GetCategoryRowsAsync(userId, period.From, period.To, cancellationToken);
        var quarters = await analytics.GetQuarterHourRowsAsync(userId, period.From, period.To, cancellationToken);

        var foods = await analytics.GetTopFoodRowsAsync(
            userId, period.From, period.To, GetIntakeAnalysis.GetIntakeAnalysisHandler.TopFoodCount, cancellationToken);

        var summary = intakeAnalyser.Summarise(days, period.TotalDays, previousDays);

        var figures = new AnalyticsFigures(
            period,
            summary,
            intakeAnalyser.BreakDownByMeal(meals),
            intakeAnalyser.BreakDownByCategory(categories),
            intakeAnalyser.TopFoods(foods, summary.TotalKilocalories),
            macroAnalyser.Analyse(days),
            patternAnalyser.ByWeekday(days),
            patternAnalyser.ByHour(quarters, request.UtcOffsetMinutes),
            previousDays.Count);

        var observations = engine.Observe(figures);

        return new ObservationsResponse(
            DietAnalyticsMapper.ToDto(period),
            [.. observations.Select(DietAnalyticsMapper.ToDto)],

            // Stated rather than left for a client to infer from an empty list (FR-021), and
            // accompanied by what it would take to see something (FR-018).
            NothingStoodOut: observations.Count == 0,
            engine.MinimumDaysForAnyObservation);
    }
}
