using DietApi.Domain;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.Exercise.UpdateExerciseEntry;

/// <summary>
/// Changes what a session was, or how long it lasted, and re-estimates.
/// </summary>
/// <remarks>
/// Returns null when the session is not the caller's, so the endpoint answers 404 - unreachable
/// rather than merely forbidden. The estimate is recomputed from the activity's current MET and
/// the member's current weight and re-snapshotted: a member's own edit is a deliberate act,
/// unlike a background correction to the catalogue, which must never rewrite history.
/// </remarks>
public record UpdateExerciseEntryCommand(Guid EntryId, UpdateExerciseEntryRequest Request) : IRequest<ExerciseDayDto?>;

public class UpdateExerciseEntryHandler(
    IDietPlanRepository planRepository,
    IExerciseDayRepository exerciseRepository,
    IActivityTypeRepository catalogue,
    IUserService userService) : IRequestHandler<UpdateExerciseEntryCommand, ExerciseDayDto?>
{
    public async Task<ExerciseDayDto?> Handle(UpdateExerciseEntryCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var day = await exerciseRepository.GetByEntryIdAsync(userId, request.EntryId, cancellationToken);
        if (day is null)
            return null;

        if (request.Request.Version != day.Version)
            throw ConcurrencyConflictException.ForDay(day.Date);

        var activity = await catalogue.GetByIdAsync(request.Request.ActivityTypeId, cancellationToken);
        if (activity is null)
            return null;

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DDD.BuildingBlocks.DomainException("Set up your diet plan before recording exercise");

        day.UpdateEntry(
            request.EntryId,
            activity.Id,
            activity.Name,
            activity.Met,
            request.Request.DurationMinutes,
            plan.CurrentWeightKg());

        await exerciseRepository.UpdateAsync(day, cancellationToken);

        return ExerciseMapper.ToDto(day);
    }
}
