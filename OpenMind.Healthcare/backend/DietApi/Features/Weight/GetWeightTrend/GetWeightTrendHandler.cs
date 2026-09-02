using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.Weight.GetWeightTrend;

public record GetWeightTrendQuery(DateOnly? From, DateOnly? To) : IRequest<WeightTrendDto?>;

public class GetWeightTrendHandler(
    IDietPlanRepository planRepository,
    IUserService userService) : IRequestHandler<GetWeightTrendQuery, WeightTrendDto?>
{
    public async Task<WeightTrendDto?> Handle(GetWeightTrendQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        // No readings in the period asked about is an empty chart, not an error.
        return WeightMapper.ToDto(plan.WeightTrend(request.From, request.To));
    }
}
