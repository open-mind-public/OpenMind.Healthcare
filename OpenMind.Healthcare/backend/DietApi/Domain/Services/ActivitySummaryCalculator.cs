using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Services;

/// <summary>
/// Turns a member's exercise days into the weekly picture they see.
/// </summary>
/// <remarks>
/// <para>
/// Days before the plan started and days after the moment being asked about are excluded from
/// every figure: they are neither activity nor the absence of it, and counting them would make
/// a member who joined on Thursday look like they had a quiet week (FR-023).
/// </para>
/// <para>
/// A day with three sessions counts once toward active days. The question is how many days a
/// member moved on, not how many times they pressed the button.
/// </para>
/// </remarks>
public class ActivitySummaryCalculator
{
    /// <summary>A week. The window and the comparison window are both this long.</summary>
    public const int WindowDays = 7;

    public ActivitySummary Summarise(
        IReadOnlyList<ExerciseDaySummary> days,
        DateOnly planStartDate,
        DateTime? asOf = null)
    {
        var today = DateOnly.FromDateTime(asOf ?? DateTime.UtcNow);

        var currentFrom = today.AddDays(-(WindowDays - 1));
        var previousTo = currentFrom.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(WindowDays - 1));

        var current = InWindow(days, currentFrom, today, planStartDate);
        var previous = InWindow(days, previousFrom, previousTo, planStartDate);

        if (current.Count == 0 && previous.Count == 0)
            return ActivitySummary.Empty(WindowDays);

        return ActivitySummary.Create(
            WindowDays,
            activeDays: current.Count,
            totalMinutes: current.Sum(d => d.TotalMinutes),
            totalKilocalories: current.Sum(d => d.TotalKilocalories),
            previousWindowActiveDays: previous.Count,
            previousWindowMinutes: previous.Sum(d => d.TotalMinutes));
    }

    /// <summary>
    /// Days in the window that are on the plan and actually had activity. The repository only
    /// returns days with sessions, but the entry count is checked anyway - a day that somehow
    /// survived with none must not count as an active one.
    /// </summary>
    private static List<ExerciseDaySummary> InWindow(
        IReadOnlyList<ExerciseDaySummary> days, DateOnly from, DateOnly to, DateOnly planStartDate) =>
        [.. days.Where(d =>
            d.EntryCount > 0
            && d.Date >= from
            && d.Date <= to
            && d.Date >= planStartDate)];
}
