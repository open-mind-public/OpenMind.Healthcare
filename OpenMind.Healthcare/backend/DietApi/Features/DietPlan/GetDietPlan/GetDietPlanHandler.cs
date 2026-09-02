using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.DietPlan.GetDietPlan;

/// <summary>
/// Returns null when the member has no plan, so the endpoint can answer 404 and the client can
/// route to setup.
/// </summary>
public record GetDietPlanQuery : IRequest<DietPlanDto?>;

public class GetDietPlanHandler(
    IDietPlanRepository planRepository,
    IUserService userService) : IRequestHandler<GetDietPlanQuery, DietPlanDto?>
{
    public async Task<DietPlanDto?> Handle(GetDietPlanQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);

        return plan is null ? null : DietPlanMapper.ToDto(plan);
    }
}
