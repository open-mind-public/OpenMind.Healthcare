using DDD.BuildingBlocks;

namespace DietApi.Domain.Entities;

/// <summary>
/// The record that a member earned an achievement, and the date they earned it.
/// </summary>
/// <remarks>
/// Stored rather than derived. A computed design cannot survive a member deleting a mis-logged
/// entry - the badge would simply vanish - and it has no way to remember the date.
/// </remarks>
public class UnlockedAchievement : Entity
{
    public Guid DietPlanId { get; private set; }
    public Guid DietAchievementId { get; private set; }
    public DateOnly EarnedOn { get; private set; }

    // Private parameterless constructor for EF Core
    private UnlockedAchievement() { }

    internal static UnlockedAchievement Earn(Guid dietPlanId, Guid achievementId, DateOnly earnedOn) =>
        new()
        {
            DietPlanId = dietPlanId,
            DietAchievementId = achievementId,
            EarnedOn = earnedOn
        };
}
