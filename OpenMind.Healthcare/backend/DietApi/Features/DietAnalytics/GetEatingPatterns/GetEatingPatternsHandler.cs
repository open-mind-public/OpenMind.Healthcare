using DDD.BuildingBlocks;
using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.DietAnalytics.GetEatingPatterns;

/// <summary>
/// When a member eats: across the week, and across their own day.
/// </summary>
/// <remarks>
/// The UTC offset arrives with the request because no member timezone is stored anywhere in this
/// application, and reaching into another bounded context for one is not permitted. It is a
/// parameter, never an ambient fact.
/// </remarks>
public record GetEatingPatternsQuery(PeriodPreset Preset, int UtcOffsetMinutes)
    : IRequest<EatingPatternsResponse?>;

public class GetEatingPatternsHandler(
    IDietPlanRepository planRepository,
    IDietAnalyticsRepository analytics,
    AnalysisPeriodResolver resolver,
    PatternAnalyser analyser,
    IUserService userService) : IRequestHandler<GetEatingPatternsQuery, EatingPatternsResponse?>
{
    /// <summary>
    /// The real world spans UTC-12:00 to UTC+14:00. An offset outside that is a client bug or a
    /// probe, and applying it silently would produce a plausible-looking chart of nonsense.
    /// </summary>
    public const int MinimumOffsetMinutes = -12 * 60;
    public const int MaximumOffsetMinutes = 14 * 60;

    public async Task<EatingPatternsResponse?> Handle(
        GetEatingPatternsQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        if (request.UtcOffsetMinutes is < MinimumOffsetMinutes or > MaximumOffsetMinutes)
            throw new DomainException("That time zone offset is not a real one");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var period = resolver.Resolve(request.Preset, plan.StartDate);

        var days = await analytics.GetDayRowsAsync(userId, period.From, period.To, cancellationToken);
        period = period.WithLoggedDays(days.Count);

        var quarters = await analytics.GetQuarterHourRowsAsync(userId, period.From, period.To, cancellationToken);

        var byWeekday = analyser.ByWeekday(days);
        var byHour = analyser.ByHour(quarters, request.UtcOffsetMinutes);

        return new EatingPatternsResponse(
            DietAnalyticsMapper.ToDto(period),
            byHour.UtcOffsetMinutes,
            byHour.IsApproximate,
            byHour.ApproximationReason,
            [.. byWeekday.Shares.Select(s => new WeekdayShareDto(s.DayOfWeek, s.AverageKilocalories, s.LoggedDays))],
            [.. byHour.Shares.Select(s => new HourShareDto(s.Hour, s.Kilocalories, s.ShareOfTotal))]);
    }
}
