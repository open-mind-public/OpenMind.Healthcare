using DDD.BuildingBlocks;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.Weight.DeleteWeightReading;

/// <summary>
/// Removes the reading for a date. The aggregate refuses when it is the plan's only one - the
/// suggested target is calculated from current weight, so that value must keep a source.
/// </summary>
public record DeleteWeightReadingCommand(DateOnly Date) : IRequest<bool>;

public class DeleteWeightReadingHandler(
    IDietPlanRepository planRepository,
    IUserService userService) : IRequestHandler<DeleteWeightReadingCommand, bool>
{
    public async Task<bool> Handle(DeleteWeightReadingCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Set up your diet plan before removing a weight reading");

        var removed = plan.RemoveWeightReading(request.Date);
        if (!removed)
            return false;

        await planRepository.UpdateAsync(plan, cancellationToken);
        return true;
    }
}
