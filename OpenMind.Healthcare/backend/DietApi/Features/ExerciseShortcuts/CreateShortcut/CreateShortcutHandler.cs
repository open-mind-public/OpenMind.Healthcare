using DDD.BuildingBlocks;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.ExerciseShortcuts.CreateShortcut;

/// <summary>
/// Saves a shortcut. Null when the activity is not in the catalogue, so the endpoint can answer 404.
/// </summary>
/// <remarks>
/// The activity is resolved here only to derive a default name and to prove it exists. Nothing
/// about it is stored on the shortcut — the MET and the estimate are read again when a session is
/// actually recorded (FR-010).
/// </remarks>
public record CreateShortcutCommand(CreateShortcutRequest Request) : IRequest<ExerciseShortcutListResponse?>;

public class CreateShortcutHandler(
    IDietPlanRepository planRepository,
    IActivityTypeRepository catalogue,
    ShortcutListBuilder list,
    IUserService userService) : IRequestHandler<CreateShortcutCommand, ExerciseShortcutListResponse?>
{
    public async Task<ExerciseShortcutListResponse?> Handle(
        CreateShortcutCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Set up your diet plan before saving shortcuts");

        var activity = await catalogue.GetByIdAsync(request.Request.ActivityTypeId, cancellationToken);
        if (activity is null)
            return null;

        var name = string.IsNullOrWhiteSpace(request.Request.Name)
            ? ExerciseShortcutMapper.DefaultName(activity.Name, request.Request.DurationMinutes)
            : request.Request.Name;

        plan.SaveExerciseShortcut(activity.Id, request.Request.DurationMinutes, name);

        await planRepository.UpdateAsync(plan, cancellationToken);

        return await list.BuildAsync(plan, cancellationToken);
    }
}
