using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.ExerciseShortcuts.GetShortcuts;

/// <summary>
/// A member's shortcuts, in their chosen order. Null when they have no plan.
/// </summary>
public record GetShortcutsQuery : IRequest<ExerciseShortcutListResponse?>;

public class GetShortcutsHandler(
    IDietPlanRepository planRepository,
    ShortcutListBuilder list,
    IUserService userService) : IRequestHandler<GetShortcutsQuery, ExerciseShortcutListResponse?>
{
    public async Task<ExerciseShortcutListResponse?> Handle(
        GetShortcutsQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        return await list.BuildAsync(plan, cancellationToken);
    }
}
