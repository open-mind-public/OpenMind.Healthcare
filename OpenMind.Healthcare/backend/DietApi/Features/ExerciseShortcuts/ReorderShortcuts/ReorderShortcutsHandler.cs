using DDD.BuildingBlocks;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.ExerciseShortcuts.ReorderShortcuts;

/// <summary>
/// Rearranges a member's shortcuts into the order they sent.
/// </summary>
/// <remarks>
/// Takes the complete list. A list that is not exactly the member's shortcuts is refused by the
/// aggregate rather than silently producing an order nobody asked for.
/// </remarks>
public record ReorderShortcutsCommand(ReorderShortcutsRequest Request)
    : IRequest<ExerciseShortcutListResponse?>;

public class ReorderShortcutsHandler(
    IDietPlanRepository planRepository,
    ShortcutListBuilder list,
    IUserService userService) : IRequestHandler<ReorderShortcutsCommand, ExerciseShortcutListResponse?>
{
    public async Task<ExerciseShortcutListResponse?> Handle(
        ReorderShortcutsCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Set up your diet plan before saving shortcuts");

        plan.ReorderExerciseShortcuts(request.Request.OrderedIds);

        await planRepository.UpdateAsync(plan, cancellationToken);

        return await list.BuildAsync(plan, cancellationToken);
    }
}
