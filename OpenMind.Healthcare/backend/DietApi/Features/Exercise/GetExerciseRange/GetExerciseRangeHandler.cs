using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.Exercise.GetExerciseRange;

/// <summary>
/// One summary row per day <em>that has activity</em>, for the calendar.
/// </summary>
/// <remarks>
/// <para>
/// Returns only days with sessions on them. Absence means no exercise, which is what lets the
/// calendar mark days without inventing a state for the rest - there is no fourth day state and
/// no "no exercise" row to be confused with one (research.md R-009).
/// </para>
/// <para>
/// Never loads the sessions themselves. A member with three years of history would otherwise pull
/// thousands of rows to draw a month.
/// </para>
/// </remarks>
public record GetExerciseRangeQuery(DateOnly From, DateOnly To) : IRequest<ExerciseRangeResponse?>;

public class GetExerciseRangeHandler(
    IDietPlanRepository planRepository,
    IExerciseDayRepository exerciseRepository,
    IUserService userService) : IRequestHandler<GetExerciseRangeQuery, ExerciseRangeResponse?>
{
    public async Task<ExerciseRangeResponse?> Handle(GetExerciseRangeQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var summaries = await exerciseRepository.GetRangeAsync(userId, request.From, request.To, cancellationToken);

        // Days outside the plan are excluded entirely rather than reported as empty: they are
        // neither activity nor the absence of it.
        var days = summaries
            .Where(s => s.Date >= plan.StartDate && s.Date <= today)
            .Select(ExerciseMapper.ToDto)
            .ToList();

        return new ExerciseRangeResponse(request.From, request.To, days);
    }
}
