using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.FoodLog.GetDayRange;

/// <summary>
/// One summary row per day for the calendar. Never loads entries - a member with three years of
/// history would otherwise pull thousands of rows to draw a month.
/// </summary>
public record GetDayRangeQuery(DateOnly From, DateOnly To) : IRequest<DayRangeResponse?>;

public class GetDayRangeHandler(
    IDietPlanRepository planRepository,
    ILoggedDayRepository dayRepository,
    IUserService userService) : IRequestHandler<GetDayRangeQuery, DayRangeResponse?>
{
    public async Task<DayRangeResponse?> Handle(GetDayRangeQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var summaries = await dayRepository.GetRangeAsync(userId, request.From, request.To, cancellationToken);
        var byDate = summaries.ToDictionary(s => s.Date);

        var days = new List<DaySummaryDto>();

        for (var date = request.From; date <= request.To; date = date.AddDays(1))
        {
            // Outside the plan is a property of the range asked about, not a fourth day state -
            // so these rows carry no state at all rather than a misleading one.
            if (date < plan.StartDate || date > today)
            {
                days.Add(new DaySummaryDto(date, WithinPlan: false, State: null, ConsumedCalories: null, TargetCalories: null));
                continue;
            }

            days.Add(byDate.TryGetValue(date, out var summary)
                ? FoodLogMapper.ToDto(summary)
                : new DaySummaryDto(date, WithinPlan: true, Domain.ValueObjects.DayState.NotLogged, 0, plan.Targets.Calories));
        }

        return new DayRangeResponse(request.From, request.To, plan.StartDate, days);
    }
}
