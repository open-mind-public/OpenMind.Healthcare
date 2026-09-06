using DietApi.Domain.Repositories;
using DietApi.Features.BeerDays;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.BeerDays.UnmarkBeerDay;

/// <summary>
/// Removes a beer-day marking.
/// </summary>
/// <remarks>
/// Idempotent: unmarking a date that is not a beer day succeeds and does nothing. Returns null when
/// the member has no plan, so the endpoint can answer 404.
/// </remarks>
public record UnmarkBeerDayCommand(DateOnly Date) : IRequest<BeerDayResponse?>;

public class UnmarkBeerDayHandler(
    IDietPlanRepository planRepository,
    IBeerDayRepository beerDayRepository,
    IUserService userService) : IRequestHandler<UnmarkBeerDayCommand, BeerDayResponse?>
{
    public async Task<BeerDayResponse?> Handle(UnmarkBeerDayCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var existing = await beerDayRepository.GetByDateAsync(userId, request.Date, cancellationToken);
        if (existing is not null)
            await beerDayRepository.DeleteAsync(existing, cancellationToken);

        return new BeerDayResponse(request.Date, IsBeerDay: false);
    }
}
