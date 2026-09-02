using DietApi.Domain.Repositories;
using DietApi.Domain.Services;

namespace DietApi.Tests.Domain;

/// <summary>
/// Streaks are the retention mechanic, so the boundaries matter more here than anywhere else.
/// </summary>
public class StreakCalculatorTests
{
    private static readonly DateTime Clock = new(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Clock);

    private readonly StreakCalculator _calculator = new();

    [Fact]
    public void A_member_with_no_days_gets_zeros_rather_than_an_error()
    {
        var stats = _calculator.Calculate([], Today.AddDays(-30), Clock);

        stats.CurrentStreakDays.ShouldBe(0);
        stats.LongestStreakDays.ShouldBe(0);
        stats.TotalDaysLogged.ShouldBe(0);
        stats.AverageDailyCalories.ShouldBe(0);
        stats.DaysOnPlan.ShouldBe(31);
    }

    [Fact]
    public void Five_consecutive_on_target_days_read_as_a_five_day_streak()
    {
        var days = OnTarget(4, 3, 2, 1, 0);

        var stats = _calculator.Calculate(days, Today.AddDays(-30), Clock);

        stats.CurrentStreakDays.ShouldBe(5);
        stats.LongestStreakDays.ShouldBe(5);
        stats.TotalDaysLogged.ShouldBe(5);
    }

    [Fact]
    public void A_single_day_is_a_streak_of_one()
    {
        var stats = _calculator.Calculate(OnTarget(0), Today.AddDays(-30), Clock);

        stats.CurrentStreakDays.ShouldBe(1);
        stats.LongestStreakDays.ShouldBe(1);
    }

    [Fact]
    public void An_over_target_day_breaks_the_current_streak_but_not_the_best_one()
    {
        // Six good days, then one over target, then two good ones.
        var days = new List<DaySummary>();
        days.AddRange(OnTarget(10, 9, 8, 7, 6, 5));
        days.Add(new DaySummary(Today.AddDays(-4), 2600, 2100, true));
        days.AddRange(OnTarget(3, 2));

        var stats = _calculator.Calculate(days, Today.AddDays(-30), Clock);

        // Today itself is not logged, so the current run has already stopped.
        stats.CurrentStreakDays.ShouldBe(0);
        stats.LongestStreakDays.ShouldBe(6);
    }

    [Fact]
    public void An_unlogged_day_interrupts_a_streak_just_as_an_over_target_day_does()
    {
        // This is the rule most trackers get wrong: skipping a day is not a free pass.
        var days = OnTarget(4, 3, 1, 0);   // day 2 missing entirely

        var stats = _calculator.Calculate(days, Today.AddDays(-30), Clock);

        stats.CurrentStreakDays.ShouldBe(2);
        stats.LongestStreakDays.ShouldBe(2);
    }

    [Fact]
    public void A_streak_running_up_to_today_is_the_current_streak()
    {
        var stats = _calculator.Calculate(OnTarget(2, 1, 0), Today.AddDays(-30), Clock);

        stats.CurrentStreakDays.ShouldBe(3);
    }

    [Fact]
    public void A_streak_that_ended_yesterday_is_no_longer_current()
    {
        var stats = _calculator.Calculate(OnTarget(3, 2, 1), Today.AddDays(-30), Clock);

        stats.CurrentStreakDays.ShouldBe(0);
        stats.LongestStreakDays.ShouldBe(3);
    }

    [Fact]
    public void Days_before_the_plan_started_are_excluded_from_every_figure()
    {
        var planStart = Today.AddDays(-3);

        var days = new List<DaySummary>
        {
            new(Today.AddDays(-10), 1800, 2100, true),   // before the plan
            new(Today.AddDays(-9), 1900, 2100, true),    // before the plan
            new(Today.AddDays(-2), 1800, 2100, true),
            new(Today.AddDays(-1), 1900, 2100, true),
            new(Today, 2000, 2100, true)
        };

        var stats = _calculator.Calculate(days, planStart, Clock);

        stats.TotalDaysLogged.ShouldBe(3);
        stats.CurrentStreakDays.ShouldBe(3);
        stats.LongestStreakDays.ShouldBe(3);
    }

    [Fact]
    public void Days_after_the_moment_asked_about_are_excluded_too()
    {
        var days = new List<DaySummary>
        {
            new(Today, 2000, 2100, true),
            new(Today.AddDays(1), 1500, 2100, true)   // in the future relative to the clock
        };

        var stats = _calculator.Calculate(days, Today.AddDays(-30), Clock);

        stats.TotalDaysLogged.ShouldBe(1);
    }

    [Fact]
    public void A_range_spanning_a_leap_day_counts_it_like_any_other()
    {
        var leapClock = new DateTime(2028, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var leapToday = DateOnly.FromDateTime(leapClock);

        var days = new List<DaySummary>
        {
            new(new DateOnly(2028, 2, 28), 1800, 2100, true),
            new(new DateOnly(2028, 2, 29), 1900, 2100, true),   // the leap day
            new(new DateOnly(2028, 3, 1), 2000, 2100, true)
        };

        var stats = _calculator.Calculate(days, new DateOnly(2028, 2, 1), leapClock);

        stats.CurrentStreakDays.ShouldBe(3);
        stats.TotalDaysLogged.ShouldBe(3);
        stats.DaysOnPlan.ShouldBe(30);   // February 2028 has 29 days, plus 1 March
        _ = leapToday;
    }

    [Fact]
    public void An_all_unlogged_history_reads_as_no_streak_at_all()
    {
        var days = new List<DaySummary>
        {
            new(Today.AddDays(-2), 0, 2100, false),
            new(Today.AddDays(-1), 0, 2100, false)
        };

        var stats = _calculator.Calculate(days, Today.AddDays(-30), Clock);

        stats.CurrentStreakDays.ShouldBe(0);
        stats.LongestStreakDays.ShouldBe(0);
        stats.TotalDaysLogged.ShouldBe(0);
        stats.AverageDailyCalories.ShouldBe(0);
    }

    [Fact]
    public void Average_intake_covers_only_logged_days()
    {
        var days = new List<DaySummary>
        {
            new(Today.AddDays(-2), 2000, 2100, true),
            new(Today.AddDays(-1), 0, 2100, false),      // not logged - must not drag the average to zero
            new(Today, 2200, 2100, true)
        };

        var stats = _calculator.Calculate(days, Today.AddDays(-30), Clock);

        stats.AverageDailyCalories.ShouldBe(2100);
        stats.AverageWindowDays.ShouldBe(2);
    }

    [Fact]
    public void Average_intake_looks_back_no_further_than_the_window()
    {
        // 40 logged days, the oldest ten wildly different from the rest.
        var days = new List<DaySummary>();
        for (var i = 39; i >= 0; i--)
        {
            days.Add(new DaySummary(Today.AddDays(-i), i >= 30 ? 5000 : 2000, 2100, true));
        }

        var stats = _calculator.Calculate(days, Today.AddDays(-60), Clock);

        stats.AverageWindowDays.ShouldBe(StreakCalculator.AverageWindowDays);
        stats.AverageDailyCalories.ShouldBe(2000);
    }

    private static List<DaySummary> OnTarget(params int[] daysAgo) =>
        [.. daysAgo.Select(d => new DaySummary(Today.AddDays(-d), 1800, 2100, true))];
}
