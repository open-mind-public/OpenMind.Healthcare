using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.DietAnalytics.GetHabitInsights;

/// <summary>
/// How often the member logs beer and exercise over the period, and how their eating on beer days
/// compares with every other day.
/// </summary>
/// <remarks>
/// The handler only gathers inputs - the resolved period, the logged-day rows already used by the
/// rest of analytics, the beer dates, and the exercise dates - and hands them to
/// <see cref="HabitAnalyser"/>. Every count and every comparison is the analyser's, so it is tested
/// without a database (Principle II).
/// </remarks>
public record GetHabitInsightsQuery(PeriodPreset Preset) : IRequest<HabitInsightsResponse?>;

public class GetHabitInsightsHandler(
    IDietPlanRepository planRepository,
    IDietAnalyticsRepository analytics,
    IBeerDayRepository beerDayRepository,
    IExerciseDayRepository exerciseRepository,
    AnalysisPeriodResolver resolver,
    HabitAnalyser analyser,
    IUserService userService) : IRequestHandler<GetHabitInsightsQuery, HabitInsightsResponse?>
{
    public async Task<HabitInsightsResponse?> Handle(GetHabitInsightsQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var period = resolver.Resolve(request.Preset, plan.StartDate);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var loggedDays = await analytics.GetDayRowsAsync(userId, period.From, period.To, cancellationToken);
        period = period.WithLoggedDays(loggedDays.Count);

        var beerDates = await beerDayRepository.GetDatesInRangeAsync(userId, period.From, period.To, cancellationToken);
        var exerciseDays = await exerciseRepository.GetRangeAsync(userId, period.From, period.To, cancellationToken);

        var analysis = analyser.Analyse(
            period,
            plan.StartDate,
            today,
            loggedDays,
            beerDates.ToHashSet(),
            exerciseDays.Select(d => d.Date).ToHashSet());

        return DietAnalyticsMapper.ToResponse(period, analysis);
    }
}
