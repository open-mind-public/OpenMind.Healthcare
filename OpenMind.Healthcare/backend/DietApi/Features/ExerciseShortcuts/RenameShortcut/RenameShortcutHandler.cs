using DDD.BuildingBlocks;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.ExerciseShortcuts.RenameShortcut;

/// <summary>
/// Renames a shortcut. Null when it is not the caller's, so the endpoint can answer 404.
/// </summary>
public record RenameShortcutCommand(Guid ShortcutId, RenameShortcutRequest Request)
    : IRequest<ExerciseShortcutListResponse?>;

public class RenameShortcutHandler(
    IDietPlanRepository planRepository,
    ShortcutListBuilder list,
    IUserService userService) : IRequestHandler<RenameShortcutCommand, ExerciseShortcutListResponse?>
{
    public async Task<ExerciseShortcutListResponse?> Handle(
        RenameShortcutCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Set up your diet plan before saving shortcuts");

        if (!plan.RenameExerciseShortcut(request.ShortcutId, request.Request.Name))
            return null;

        await planRepository.UpdateAsync(plan, cancellationToken);

        return await list.BuildAsync(plan, cancellationToken);
    }
}
