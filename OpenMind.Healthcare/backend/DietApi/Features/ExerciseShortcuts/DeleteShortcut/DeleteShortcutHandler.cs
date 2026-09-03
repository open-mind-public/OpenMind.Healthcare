using DDD.BuildingBlocks;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.ExerciseShortcuts.DeleteShortcut;

/// <summary>
/// Removes a shortcut. Null when it is not the caller's, so the endpoint can answer 404.
/// </summary>
/// <remarks>
/// Removes a button, never a session. Everything recorded from this shortcut is untouched — the
/// entries carry their own snapshots and have no link back here (FR-017).
/// </remarks>
public record DeleteShortcutCommand(Guid ShortcutId) : IRequest<ExerciseShortcutListResponse?>;

public class DeleteShortcutHandler(
    IDietPlanRepository planRepository,
    ShortcutListBuilder list,
    IUserService userService) : IRequestHandler<DeleteShortcutCommand, ExerciseShortcutListResponse?>
{
    public async Task<ExerciseShortcutListResponse?> Handle(
        DeleteShortcutCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Set up your diet plan before saving shortcuts");

        if (!plan.RemoveExerciseShortcut(request.ShortcutId))
            return null;

        await planRepository.UpdateAsync(plan, cancellationToken);

        return await list.BuildAsync(plan, cancellationToken);
    }
}
