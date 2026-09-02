using DDD.BuildingBlocks;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.DietPlan.SetTargets;

/// <summary>
/// The only path that changes the targets in force. Days already logged keep the target that was
/// snapshotted onto them, so history does not move when this runs.
/// </summary>
public record SetDietTargetsCommand(SetTargetsRequest Request) : IRequest<DietPlanResponse>;

public class SetDietTargetsHandler(
    IDietPlanRepository planRepository,
    IUserService userService) : IRequestHandler<SetDietTargetsCommand, DietPlanResponse>
{
    public async Task<DietPlanResponse> Handle(SetDietTargetsCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Set up your diet plan before setting targets");

        var targets = DietPlanMapper.ToDomain(request.Request.Targets);

        plan.SetTargets(targets, request.Request.TargetSource);

        await planRepository.UpdateAsync(plan, cancellationToken);

        return new DietPlanResponse(
            DietPlanMapper.ToDto(plan),
            DietPlanMapper.BelowFloorWarning(targets, request.Request.TargetSource, plan.BodyMetrics.Sex));
    }
}
