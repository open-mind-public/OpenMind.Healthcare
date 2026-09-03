using DDD.BuildingBlocks;
using DietApi.Domain;
using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.Exercise.AddExerciseEntry;

/// <summary>
/// Records a session, creating the exercise day if this is the date's first.
/// </summary>
/// <remarks>
/// <para>
/// Returns null when the activity is not in the catalogue, so the endpoint can answer 404. A
/// member without a plan is a different failure - they are told to set one up (400), because
/// exercise is bounded by the plan's start date and the estimate needs the plan's weight.
/// </para>
/// <para>
/// The estimate is computed here from the activity's MET, the duration and the member's
/// <em>current</em> weight, then snapshotted onto the entry and never recomputed. Stepping on
/// the scales tomorrow does not rewrite what today's run appeared to cost (FR-009).
/// </para>
/// <para>
/// Note what this handler does not do: it does not load the member's logged day, touch their
/// target, or adjust their activity level. Recording exercise leaves the eating assessment
/// exactly where it was (FR-015, FR-018).
/// </para>
/// </remarks>
public record AddExerciseEntryCommand(DateOnly Date, AddExerciseEntryRequest Request) : IRequest<ExerciseDayDto?>;

public class AddExerciseEntryHandler(
    IDietPlanRepository planRepository,
    IExerciseDayRepository exerciseRepository,
    IActivityTypeRepository catalogue,
    IUserService userService) : IRequestHandler<AddExerciseEntryCommand, ExerciseDayDto?>
{
    public async Task<ExerciseDayDto?> Handle(AddExerciseEntryCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Set up your diet plan before recording exercise");

        var activity = await catalogue.GetByIdAsync(request.Request.ActivityTypeId, cancellationToken);
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
            request.Request.DurationMinutes,
            plan.CurrentWeightKg());

        if (isNewDay)
            await exerciseRepository.AddAsync(day, cancellationToken);
        else
            await exerciseRepository.UpdateAsync(day, cancellationToken);

        return ExerciseMapper.ToDto(day);
    }
}
