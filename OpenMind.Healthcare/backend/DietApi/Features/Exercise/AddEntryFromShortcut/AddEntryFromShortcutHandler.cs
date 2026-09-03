using DDD.BuildingBlocks;
using DietApi.Domain;
using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.Exercise.AddEntryFromShortcut;

/// <summary>
/// The one tap: records the session a shortcut stands for.
/// </summary>
/// <remarks>
/// <para>
/// This deliberately does the same things the by-hand path does, in the same order, and ends in the
/// same <c>ExerciseDay.AddEntry</c>. The shortcut contributes only the two values a member would
/// otherwise have typed — which activity, and for how long. Everything after that is identical, and
/// a test records the same session both ways and compares the results field by field.
/// </para>
/// <para>
/// In particular the estimate is computed <em>here</em>, from the activity's current energy rate and
/// the member's current weight. Nothing is read off the shortcut, because a shortcut that carried an
/// estimate would freeze the member's weight at the moment they saved it (FR-010).
/// </para>
/// <para>
/// No rule is relaxed. Future dates, dates before the plan started, the duration bounds and the
/// day's concurrency token all behave exactly as they do for a typed session (FR-012).
/// </para>
/// </remarks>
public record AddEntryFromShortcutCommand(DateOnly Date, AddEntryFromShortcutRequest Request)
    : IRequest<ExerciseDayDto?>;

public class AddEntryFromShortcutHandler(
    IDietPlanRepository planRepository,
    IExerciseDayRepository exerciseRepository,
    IActivityTypeRepository catalogue,
    IUserService userService) : IRequestHandler<AddEntryFromShortcutCommand, ExerciseDayDto?>
{
    public async Task<ExerciseDayDto?> Handle(
        AddEntryFromShortcutCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Set up your diet plan before recording exercise");

        // Looked up on the member's own plan, so another member's shortcut is unreachable rather
        // than merely forbidden.
        var shortcut = plan.ExerciseShortcut(request.Request.ShortcutId);
        if (shortcut is null)
            return null;

        var activity = await catalogue.GetByIdAsync(shortcut.ActivityTypeId, cancellationToken);
        if (activity is null)
            return null;

        var day = await exerciseRepository.GetByDateAsync(userId, request.Date, cancellationToken);
        var isNewDay = day is null;

        if (day is null)
        {
            day = ExerciseDay.StartDay(plan.Id, userId, request.Date, plan.StartDate);
        }
        else if (request.Request.Version != day.Version)
        {
            throw ConcurrencyConflictException.ForDay(request.Date);
        }

        day.AddEntry(
            activity.Id,
            activity.Name,
            activity.Met,
            shortcut.DurationMinutes,
            plan.CurrentWeightKg());

        if (isNewDay)
            await exerciseRepository.AddAsync(day, cancellationToken);
        else
            await exerciseRepository.UpdateAsync(day, cancellationToken);

        return ExerciseMapper.ToDto(day);
    }
}
