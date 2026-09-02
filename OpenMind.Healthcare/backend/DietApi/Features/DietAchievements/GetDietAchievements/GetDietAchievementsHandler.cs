using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.DietAchievements.GetDietAchievements;

/// <summary>All achievement definitions with this member's state against each.</summary>
public record GetDietAchievementsQuery(bool UnlockedOnly = false) : IRequest<DietAchievementListResponse?>;

public class GetDietAchievementsHandler(
    IDietPlanRepository planRepository,
    ILoggedDayRepository dayRepository,
    IDietAchievementRepository achievementRepository,
    StreakCalculator streakCalculator,
    DietAchievementStatusService statusService,
    IUserService userService) : IRequestHandler<GetDietAchievementsQuery, DietAchievementListResponse?>
{
    public async Task<DietAchievementListResponse?> Handle(
        GetDietAchievementsQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var summaries = await dayRepository.GetRangeAsync(userId, plan.StartDate, today, cancellationToken);
        var stats = streakCalculator.Calculate(summaries, plan.StartDate);
        var definitions = await achievementRepository.GetAllAsync(cancellationToken);

        var statuses = statusService.Evaluate(plan, stats, definitions);

        var dtos = statuses
            .Where(s => !request.UnlockedOnly || s.Unlocked)
            .OrderByDescending(s => s.EarnedOn)
            .Select(DietAchievementMapper.ToDto)
            .ToList();

        return new DietAchievementListResponse(dtos);
    }
}
