using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.DietStats.GetDietStats;

/// <summary>
/// Consistency figures for the whole plan to date. A member with a plan and no entries gets
/// zeros, not an error - an empty history is a beginning, not a failure.
/// </summary>
public record GetDietStatsQuery : IRequest<DietStatsDto?>;

public class GetDietStatsHandler(
    IDietPlanRepository planRepository,
    ILoggedDayRepository dayRepository,
    StreakCalculator streakCalculator,
    IUserService userService) : IRequestHandler<GetDietStatsQuery, DietStatsDto?>
{
    public async Task<DietStatsDto?> Handle(GetDietStatsQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var summaries = await dayRepository.GetRangeAsync(userId, plan.StartDate, today, cancellationToken);

        return DietStatsMapper.ToDto(streakCalculator.Calculate(summaries, plan.StartDate));
    }
}
