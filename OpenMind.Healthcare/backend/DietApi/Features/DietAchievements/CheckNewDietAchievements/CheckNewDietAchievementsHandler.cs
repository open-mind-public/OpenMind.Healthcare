using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.DietAchievements.CheckNewDietAchievements;

/// <summary>
/// Evaluates criteria and persists anything newly met, returning only the new ones so the client
/// can celebrate them. Idempotent - calling it twice awards nothing the second time.
/// </summary>
public record CheckNewDietAchievementsCommand : IRequest<NewlyUnlockedResponse?>;

public class CheckNewDietAchievementsHandler(
    IDietPlanRepository planRepository,
    ILoggedDayRepository dayRepository,
    IDietAchievementRepository achievementRepository,
    StreakCalculator streakCalculator,
    DietAchievementStatusService statusService,
    IUserService userService) : IRequestHandler<CheckNewDietAchievementsCommand, NewlyUnlockedResponse?>
{
    public async Task<NewlyUnlockedResponse?> Handle(
        CheckNewDietAchievementsCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var alreadyEarned = plan.UnlockedAchievements
            .Select(u => u.DietAchievementId)
            .ToHashSet();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var summaries = await dayRepository.GetRangeAsync(userId, plan.StartDate, today, cancellationToken);
        var stats = streakCalculator.Calculate(summaries, plan.StartDate);
        var definitions = await achievementRepository.GetAllAsync(cancellationToken);

        var statuses = statusService.Evaluate(plan, stats, definitions);

        var newlyUnlocked = statuses
            .Where(s => s.Unlocked && !alreadyEarned.Contains(s.Achievement.Id))
            .Select(DietAchievementMapper.ToDto)
            .ToList();

        if (newlyUnlocked.Count > 0)
            await planRepository.UpdateAsync(plan, cancellationToken);

        return new NewlyUnlockedResponse(newlyUnlocked);
    }
}
