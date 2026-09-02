using DDD.BuildingBlocks;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Aggregates;

/// <summary>
/// A named milestone a member can reach. Seeded reference data - the definitions are the same for
/// everyone; what differs per member is whether and when they earned it.
/// </summary>
public class DietAchievement : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public AchievementCriterion Criterion { get; private set; }
    public int Threshold { get; private set; }

    private DietAchievement() { }

    public static DietAchievement Create(
        string name, string description, string icon, AchievementCriterion criterion, int threshold)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("An achievement needs a name");

        if (threshold <= 0)
            throw new DomainException("An achievement threshold must be greater than zero");

        return new DietAchievement
        {
            Name = name,
            Description = description,
            Icon = icon,
            Criterion = criterion,
            Threshold = threshold
        };
    }

    /// <summary>How far along a member is against this achievement's criterion.</summary>
    public int ProgressFrom(DietStatistics stats) => Criterion switch
    {
        AchievementCriterion.ConsecutiveOnTargetDays => Math.Max(stats.CurrentStreakDays, stats.LongestStreakDays),
        AchievementCriterion.TotalDaysLogged => stats.TotalDaysLogged,
        AchievementCriterion.DaysOnPlan => stats.DaysOnPlan,
        _ => 0
    };

    public bool IsMetBy(DietStatistics stats) => ProgressFrom(stats) >= Threshold;

    public int RemainingFor(DietStatistics stats) => Math.Max(0, Threshold - ProgressFrom(stats));
}
