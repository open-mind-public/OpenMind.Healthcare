using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.DietAnalytics.GetIntakeTrend;

/// <summary>
/// A member's intake day by day, for the trend chart.
/// </summary>
/// <remarks>
/// Reads the same per-day rows the other sections use - no new query - and lays them across every
/// calendar day so unlogged days can be drawn as gaps rather than closed up.
/// </remarks>
public record GetIntakeTrendQuery(PeriodPreset Preset) : IRequest<IntakeTrendResponse?>;

public class GetIntakeTrendHandler(
    IDietPlanRepository planRepository,
    IDietAnalyticsRepository analytics,
    AnalysisPeriodResolver resolver,
    TrendAnalyser analyser,
    IUserService userService) : IRequestHandler<GetIntakeTrendQuery, IntakeTrendResponse?>
{
    public async Task<IntakeTrendResponse?> Handle(
        GetIntakeTrendQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var period = resolver.Resolve(request.Preset, plan.StartDate);
        var days = await analytics.GetDayRowsAsync(userId, period.From, period.To, cancellationToken);
        period = period.WithLoggedDays(days.Count);

        var trend = analyser.Build(period, days);

        return new IntakeTrendResponse(
            DietAnalyticsMapper.ToDto(period),
            trend.LoggedDays,
            trend.PeakCalories,
            [.. trend.Points.Select(DietAnalyticsMapper.ToDto)]);
    }
}
