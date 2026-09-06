using DietApi.Domain.Repositories;
using DietApi.Features.BeerDays;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.BeerDays.GetBeerDayRange;

/// <summary>
/// The beer days in a window, for the calendar.
/// </summary>
/// <remarks>
/// Returns only the dates that are beer days. A date that is not in the list is not a beer day -
/// there is no "not a beer day" row, the same way the exercise range has none (research.md R-003).
/// Dates outside the plan are dropped: they are neither a beer day within this plan nor the absence
/// of one.
/// </remarks>
public record GetBeerDayRangeQuery(DateOnly From, DateOnly To) : IRequest<BeerDayRangeResponse?>;

public class GetBeerDayRangeHandler(
    IDietPlanRepository planRepository,
    IBeerDayRepository beerDayRepository,
    IUserService userService) : IRequestHandler<GetBeerDayRangeQuery, BeerDayRangeResponse?>
{
    public async Task<BeerDayRangeResponse?> Handle(GetBeerDayRangeQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dates = await beerDayRepository.GetDatesInRangeAsync(userId, request.From, request.To, cancellationToken);

        var withinPlan = dates
            .Where(d => d >= plan.StartDate && d <= today)
            .ToList();

        return new BeerDayRangeResponse(request.From, request.To, withinPlan);
    }
}
