using DietApi.Domain.ValueObjects;

namespace DietApi.Features.DietStats;

public record DietStatsDto(
    int CurrentStreakDays,
    int LongestStreakDays,
    int TotalDaysLogged,
    int AverageDailyCalories,
    int AverageWindowDays,
    DateOnly PlanStartDate,
    int DaysOnPlan);

public static class DietStatsMapper
{
    public static DietStatsDto ToDto(DietStatistics stats) =>
        new(stats.CurrentStreakDays,
            stats.LongestStreakDays,
            stats.TotalDaysLogged,
            stats.AverageDailyCalories,
            stats.AverageWindowDays,
            stats.PlanStartDate,
            stats.DaysOnPlan);
}
