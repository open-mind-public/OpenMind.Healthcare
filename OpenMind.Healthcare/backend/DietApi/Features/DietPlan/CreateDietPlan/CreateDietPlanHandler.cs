using DDD.BuildingBlocks;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;
using DietPlanAggregate = DietApi.Domain.Aggregates.DietPlan;

namespace DietApi.Features.DietPlan.CreateDietPlan;

public record CreateDietPlanCommand(CreateDietPlanRequest Request) : IRequest<DietPlanResponse>;

public class CreateDietPlanHandler(
    IDietPlanRepository planRepository,
    IUserService userService) : IRequestHandler<CreateDietPlanCommand, DietPlanResponse>
{
    public async Task<DietPlanResponse> Handle(CreateDietPlanCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        if (await planRepository.GetByUserIdAsync(userId, cancellationToken) is not null)
            throw new DomainException("You already have a diet plan. Update it instead of creating another.");

        var r = request.Request;
        var bodyMetrics = DietPlanMapper.ToDomain(r.BodyMetrics);
        var targets = DietPlanMapper.ToDomain(r.Targets);

        // The weight supplied here becomes the plan's first reading, so current weight has
        // exactly one source of truth from the moment the plan exists.
        var plan = DietPlanAggregate.Create(
            userId,
            r.Goal,
            r.StartDate,
            bodyMetrics,
            r.ActivityLevel,
            targets,
            r.TargetSource,
            r.CurrentWeightKg,
            r.TargetWeightKg);

        await planRepository.AddAsync(plan, cancellationToken);

        return new DietPlanResponse(
            DietPlanMapper.ToDto(plan),
            DietPlanMapper.BelowFloorWarning(targets, r.TargetSource, bodyMetrics.Sex));
    }
}
