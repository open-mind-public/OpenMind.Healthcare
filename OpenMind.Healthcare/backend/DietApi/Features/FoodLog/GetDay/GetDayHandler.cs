using DDD.BuildingBlocks;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.FoodLog.GetDay;

/// <summary>
/// Returns null when the member has no plan, so the endpoint can answer 404 and the client can
/// route to setup. A date with no entries is a logged-nothing day, not a 404.
/// </summary>
public record GetDayQuery(DateOnly Date) : IRequest<LoggedDayDto?>;

public class GetDayHandler(
    IDietPlanRepository planRepository,
    ILoggedDayRepository dayRepository,
    IUserService userService) : IRequestHandler<GetDayQuery, LoggedDayDto?>
{
    public async Task<LoggedDayDto?> Handle(GetDayQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        // Out-of-plan dates are refused outright rather than described by a fourth day state.
        if (request.Date > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new DomainException("You cannot view a future date");

        if (request.Date < plan.StartDate)
            throw new DomainException($"That date is before your plan started on {plan.StartDate:yyyy-MM-dd}");

        var day = await dayRepository.GetByDateAsync(userId, request.Date, cancellationToken);

        return day is null
            ? FoodLogMapper.EmptyDay(request.Date, plan.Targets)
            : FoodLogMapper.ToDto(day);
    }
}
