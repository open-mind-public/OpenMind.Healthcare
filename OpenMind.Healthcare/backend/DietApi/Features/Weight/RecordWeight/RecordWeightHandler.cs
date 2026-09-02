using DDD.BuildingBlocks;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.Weight.RecordWeight;

/// <summary>
/// Records or replaces the reading for a date. Idempotent by design - a date holds at most one.
/// </summary>
public record RecordWeightCommand(DateOnly Date, RecordWeightRequest Request) : IRequest<WeightTrendDto>;

public class RecordWeightHandler(
    IDietPlanRepository planRepository,
    IUserService userService) : IRequestHandler<RecordWeightCommand, WeightTrendDto>
{
    public async Task<WeightTrendDto> Handle(RecordWeightCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Set up your diet plan before recording a weight");

        plan.RecordWeight(request.Date, request.Request.WeightKg);

        await planRepository.UpdateAsync(plan, cancellationToken);

        return WeightMapper.ToDto(plan.WeightTrend());
    }
}
