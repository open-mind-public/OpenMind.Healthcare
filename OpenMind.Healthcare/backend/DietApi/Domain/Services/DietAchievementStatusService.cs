using DietApi.Domain.Aggregates;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Services;

/// <summary>
/// One achievement's state for one member.
/// </summary>
public record DietAchievementStatus(
    DietAchievement Achievement,
    bool Unlocked,
    DateOnly? EarnedOn,
    int Remaining);

/// <summary>
/// Works out which achievements a member has earned, and unlocks any newly met.
/// </summary>
/// <remarks>
/// Persisted state always wins. If the plan already holds an unlock record, the achievement is
/// unlocked regardless of what the member's current statistics say - so deleting a mis-logged
/// entry can never take a badge away.
/// </remarks>
public class DietAchievementStatusService
{
    public IReadOnlyList<DietAchievementStatus> Evaluate(
        DietPlan plan,
        DietStatistics stats,
        IReadOnlyList<DietAchievement> achievements,
        DateTime? asOf = null)
    {
        var today = DateOnly.FromDateTime(asOf ?? DateTime.UtcNow);
        var earnedById = plan.UnlockedAchievements.ToDictionary(u => u.DietAchievementId, u => u.EarnedOn);

        var statuses = new List<DietAchievementStatus>();

        foreach (var achievement in achievements.OrderBy(a => a.Criterion).ThenBy(a => a.Threshold))
        {
            if (earnedById.TryGetValue(achievement.Id, out var earnedOn))
            {
                statuses.Add(new DietAchievementStatus(achievement, Unlocked: true, earnedOn, Remaining: 0));
                continue;
            }

            if (achievement.IsMetBy(stats))
            {
                plan.Unlock(achievement.Id, today);
                statuses.Add(new DietAchievementStatus(achievement, Unlocked: true, today, Remaining: 0));
                continue;
            }

            statuses.Add(new DietAchievementStatus(
                achievement, Unlocked: false, EarnedOn: null, achievement.RemainingFor(stats)));
        }

        return statuses;
    }
}
