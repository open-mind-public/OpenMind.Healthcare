using DDD.BuildingBlocks;
using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using DietApi.Features.BeerDays;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.BeerDays.MarkBeerDay;

/// <summary>
/// Marks a date as a beer day.
/// </summary>
/// <remarks>
/// Idempotent: a date that is already a beer day is left exactly as it is, and nothing is written
/// (FR-017). Returns null when the member has no plan, so the endpoint can answer 404 - beer days
/// are bounded by the plan's start date.
///
/// Note what this handler does not do: it never loads the member's logged day, their target, or
/// their streak. Marking a beer day changes nothing about how their eating is judged (FR-004, FR-010).
/// </remarks>
public record MarkBeerDayCommand(DateOnly Date) : IRequest<BeerDayResponse?>;

public class MarkBeerDayHandler(
    IDietPlanRepository planRepository,
    IBeerDayRepository beerDayRepository,
    IUserService userService) : IRequestHandler<MarkBeerDayCommand, BeerDayResponse?>
{
    public async Task<BeerDayResponse?> Handle(MarkBeerDayCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var existing = await beerDayRepository.GetByDateAsync(userId, request.Date, cancellationToken);
        if (existing is not null)
            return new BeerDayResponse(request.Date, IsBeerDay: true);

        var day = BeerDay.Mark(plan.Id, userId, request.Date, plan.StartDate);
        await beerDayRepository.AddAsync(day, cancellationToken);

        return new BeerDayResponse(request.Date, IsBeerDay: true);
    }
}
