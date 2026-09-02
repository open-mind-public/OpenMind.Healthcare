using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

/// <summary>
/// A member's consistency at a glance. Derived from their day states - never stored.
/// </summary>
public class DietStatistics : ValueObject
{
    public int CurrentStreakDays { get; private set; }
    public int LongestStreakDays { get; private set; }
    public int TotalDaysLogged { get; private set; }
    public int AverageDailyCalories { get; private set; }
    public int AverageWindowDays { get; private set; }
    public DateOnly PlanStartDate { get; private set; }
    public int DaysOnPlan { get; private set; }

    private DietStatistics() { }

    public static DietStatistics Create(
        int currentStreakDays,
        int longestStreakDays,
        int totalDaysLogged,
        int averageDailyCalories,
        int averageWindowDays,
        DateOnly planStartDate,
        int daysOnPlan) =>
        new()
        {
            CurrentStreakDays = currentStreakDays,
            LongestStreakDays = longestStreakDays,
            TotalDaysLogged = totalDaysLogged,
            AverageDailyCalories = averageDailyCalories,
            AverageWindowDays = averageWindowDays,
            PlanStartDate = planStartDate,
            DaysOnPlan = daysOnPlan
        };

    /// <summary>A member with a plan and no entries. Zeros, not an error.</summary>
    public static DietStatistics Empty(DateOnly planStartDate, int daysOnPlan) =>
        Create(0, 0, 0, 0, 0, planStartDate, daysOnPlan);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CurrentStreakDays;
        yield return LongestStreakDays;
        yield return TotalDaysLogged;
        yield return AverageDailyCalories;
        yield return PlanStartDate;
    }
}
