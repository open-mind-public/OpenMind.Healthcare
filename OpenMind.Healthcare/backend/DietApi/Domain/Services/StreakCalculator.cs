using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Services;

/// <summary>
/// Turns a member's day states into the consistency figures they see.
/// </summary>
/// <remarks>
/// The rule most habit trackers get wrong is what an unlogged day does. Here a streak counts back
/// from today through consecutive on-target days and stops at the first day that is over target
/// <em>or</em> not logged - so forgetting to log interrupts a run exactly as going over does.
/// Anything else would reward a member for logging nothing.
/// <para>
/// Days before the plan started and days after the moment being asked about are excluded from
/// every figure: they are neither successes nor misses.
/// </para>
/// </remarks>
public class StreakCalculator
{
    /// <summary>How many recent logged days the average intake is taken over.</summary>
    public const int AverageWindowDays = 30;

    public DietStatistics Calculate(
        IReadOnlyList<DaySummary> days,
        DateOnly planStartDate,
        DateTime? asOf = null)
    {
        var today = DateOnly.FromDateTime(asOf ?? DateTime.UtcNow);
        var daysOnPlan = Math.Max(0, today.DayNumber - planStartDate.DayNumber + 1);

        var inRange = days
            .Where(d => d.Date >= planStartDate && d.Date <= today)
            .OrderBy(d => d.Date)
            .ToList();

        if (inRange.Count == 0)
            return DietStatistics.Empty(planStartDate, daysOnPlan);

        var stateByDate = inRange.ToDictionary(
            d => d.Date,
            d => DayAssessment.For(d.Date, d.ConsumedCalories, d.TargetCalories, d.HasEntries).State);

        var logged = inRange.Where(d => d.HasEntries).ToList();

        var recent = logged
            .OrderByDescending(d => d.Date)
            .Take(AverageWindowDays)
            .ToList();

        // Averaging an int column, which is exactly why calories are stored as int.
        var average = recent.Count == 0
            ? 0
            : (int)Math.Round(recent.Average(d => (double)d.ConsumedCalories), MidpointRounding.AwayFromZero);

        return DietStatistics.Create(
            CurrentStreak(stateByDate, planStartDate, today),
            LongestStreak(stateByDate, planStartDate, today),
            logged.Count,
            average,
            recent.Count,
            planStartDate,
            daysOnPlan);
    }

    private static int CurrentStreak(
        IReadOnlyDictionary<DateOnly, DayState> stateByDate, DateOnly planStart, DateOnly today)
    {
        var streak = 0;

        for (var date = today; date >= planStart; date = date.AddDays(-1))
        {
            if (!stateByDate.TryGetValue(date, out var state) || state != DayState.OnTarget)
                break;

            streak++;
        }

        return streak;
    }

    private static int LongestStreak(
        IReadOnlyDictionary<DateOnly, DayState> stateByDate, DateOnly planStart, DateOnly today)
    {
        var longest = 0;
        var running = 0;

        for (var date = planStart; date <= today; date = date.AddDays(1))
        {
            if (stateByDate.TryGetValue(date, out var state) && state == DayState.OnTarget)
            {
                running++;
                longest = Math.Max(longest, running);
            }
            else
            {
                running = 0;
            }
        }

        return longest;
    }
}
