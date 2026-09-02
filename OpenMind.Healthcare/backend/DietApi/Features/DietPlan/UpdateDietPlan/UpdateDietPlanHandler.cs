using DDD.BuildingBlocks;
using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.DietPlan.UpdateDietPlan;

public record UpdateDietPlanCommand(UpdateDietPlanRequest Request) : IRequest<UpdateDietPlanResponse>;

/// <summary>
/// Updates the plan and offers a refreshed suggestion beside it.
/// </summary>
/// <remarks>
/// The targets in force are deliberately left alone. A member who changed their weight has not
/// asked for their target to change, and silently rewriting a number they chose themselves would
/// be the wrong kind of helpful. Applying the refreshed suggestion is a second, explicit step.
/// </remarks>
public class UpdateDietPlanHandler(
    IDietPlanRepository planRepository,
    TargetSuggestionService suggestionService,
    IUserService userService) : IRequestHandler<UpdateDietPlanCommand, UpdateDietPlanResponse>
{
    public async Task<UpdateDietPlanResponse> Handle(UpdateDietPlanCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Set up your diet plan before updating it");

        var r = request.Request;
        var bodyMetrics = DietPlanMapper.ToDomain(r.BodyMetrics);

        plan.UpdatePlan(r.Goal, r.StartDate, bodyMetrics, r.ActivityLevel, r.TargetWeightKg);

        await planRepository.UpdateAsync(plan, cancellationToken);

        var refreshed = suggestionService.Suggest(
            bodyMetrics,
            plan.CurrentWeightKg(),
            r.ActivityLevel,
            r.Goal);

        return new UpdateDietPlanResponse(
            DietPlanMapper.ToDto(plan),
            DietPlanMapper.ToDto(refreshed),
            TargetsUnchanged: true);
    }
}
