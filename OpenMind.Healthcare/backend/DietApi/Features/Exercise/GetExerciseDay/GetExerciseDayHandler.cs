using DDD.BuildingBlocks;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.Exercise.GetExerciseDay;

/// <summary>
/// Returns null when the member has no plan, so the endpoint can answer 404 and the client can
/// route to setup. A date with nothing recorded is an empty day, not a 404.
/// </summary>
public record GetExerciseDayQuery(DateOnly Date) : IRequest<ExerciseDayDto?>;

public class GetExerciseDayHandler(
    IDietPlanRepository planRepository,
    IExerciseDayRepository exerciseRepository,
    IUserService userService) : IRequestHandler<GetExerciseDayQuery, ExerciseDayDto?>
{
    public async Task<ExerciseDayDto?> Handle(GetExerciseDayQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        if (request.Date > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new DomainException("You cannot view a future date");

        if (request.Date < plan.StartDate)
            throw new DomainException($"That date is before your plan started on {plan.StartDate:yyyy-MM-dd}");

        var day = await exerciseRepository.GetByDateAsync(userId, request.Date, cancellationToken);

        return day is null
            ? ExerciseMapper.EmptyDay(request.Date)
            : ExerciseMapper.ToDto(day);
    }
}
