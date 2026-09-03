using DietApi.Domain.Repositories;
using DietApi.Domain.Services;

namespace DietApi.Tests.Domain;

/// <summary>
/// The weekly picture, and the boundaries where a summary usually goes wrong: the edges of the
/// window, days that predate the plan, and a day someone exercised on three times.
/// </summary>
public class ActivitySummaryTests
{
    private readonly ActivitySummaryCalculator _calculator = new();

    private static readonly DateTime Clock = new(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Clock);
    private static readonly DateOnly PlanStart = Today.AddDays(-90);

    [Fact]
    public void A_week_with_no_activity_reports_zeros_rather_than_an_error()
    {
        var summary = _calculator.Summarise([], PlanStart, Clock);

        summary.WindowDays.ShouldBe(7);
        summary.ActiveDays.ShouldBe(0);
        summary.TotalMinutes.ShouldBe(0);
        summary.TotalKilocalories.ShouldBe(0);
        summary.PreviousWindowActiveDays.ShouldBe(0);
        summary.PreviousWindowMinutes.ShouldBe(0);
    }

    [Fact]
    public void Active_days_and_totals_come_from_the_current_window()
    {
        var days = new[]
        {
            Day(0, minutes: 45, kcal: 436),
            Day(2, minutes: 30, kcal: 150),
            Day(6, minutes: 60, kcal: 500)
        };

        var summary = _calculator.Summarise(days, PlanStart, Clock);

        summary.ActiveDays.ShouldBe(3);
        summary.TotalMinutes.ShouldBe(135);
        summary.TotalKilocalories.ShouldBe(1086);
    }

    [Fact]
    public void A_day_with_several_sessions_counts_once_toward_active_days()
    {
        // The question is how many days the member moved on, not how many times they pressed the
        // button - so one day of three sessions is one active day with their combined time.
        var days = new[] { Day(1, minutes: 95, kcal: 700, entryCount: 3) };

        var summary = _calculator.Summarise(days, PlanStart, Clock);

        summary.ActiveDays.ShouldBe(1);
        summary.TotalMinutes.ShouldBe(95);
    }

    [Fact]
    public void The_window_is_the_last_seven_days_including_today()
    {
        // Six days ago is in; seven days ago belongs to the window before.
        var days = new[] { Day(6, minutes: 20, kcal: 100), Day(7, minutes: 50, kcal: 300) };

        var summary = _calculator.Summarise(days, PlanStart, Clock);

        summary.ActiveDays.ShouldBe(1);
        summary.TotalMinutes.ShouldBe(20);
        summary.PreviousWindowActiveDays.ShouldBe(1);
        summary.PreviousWindowMinutes.ShouldBe(50);
    }

    [Fact]
    public void The_previous_window_is_the_seven_days_before_that()
    {
        var days = new[]
        {
            Day(0, minutes: 30, kcal: 200),
            Day(8, minutes: 40, kcal: 250),
            Day(13, minutes: 25, kcal: 120),

            // Fourteen days ago is beyond the comparison window entirely.
            Day(14, minutes: 90, kcal: 600)
        };

        var summary = _calculator.Summarise(days, PlanStart, Clock);

        summary.ActiveDays.ShouldBe(1);
        summary.TotalMinutes.ShouldBe(30);
        summary.PreviousWindowActiveDays.ShouldBe(2);
        summary.PreviousWindowMinutes.ShouldBe(65);
    }

    [Fact]
    public void Days_before_the_plan_started_are_excluded_entirely()
    {
        // A member who joined three days ago should not look like they had a quiet week.
        var planStartedThreeDaysAgo = Today.AddDays(-3);

        var days = new[]
        {
            Day(1, minutes: 45, kcal: 400),
            Day(5, minutes: 60, kcal: 500)
        };

        var summary = _calculator.Summarise(days, planStartedThreeDaysAgo, Clock);

        summary.ActiveDays.ShouldBe(1);
        summary.TotalMinutes.ShouldBe(45);
        summary.PreviousWindowActiveDays.ShouldBe(0);
        summary.PreviousWindowMinutes.ShouldBe(0);
    }

    [Fact]
    public void Days_after_the_moment_asked_about_are_excluded()
    {
        var days = new[] { Day(-1, minutes: 90, kcal: 800), Day(0, minutes: 30, kcal: 200) };

        var summary = _calculator.Summarise(days, PlanStart, Clock);

        summary.ActiveDays.ShouldBe(1);
        summary.TotalMinutes.ShouldBe(30);
    }

    [Fact]
    public void A_window_spanning_a_leap_day_counts_it_like_any_other_day()
    {
        // 2024 is a leap year: this window runs 26 February to 3 March and contains 29 February.
        var leapClock = new DateTime(2024, 3, 3, 9, 0, 0, DateTimeKind.Utc);
        var leapToday = DateOnly.FromDateTime(leapClock);

        var days = new[]
        {
            new ExerciseDaySummary(new DateOnly(2024, 2, 29), 40, 300, 1),
            new ExerciseDaySummary(new DateOnly(2024, 2, 26), 20, 150, 1),

            // 25 February is eight days before 3 March - just outside the window.
            new ExerciseDaySummary(new DateOnly(2024, 2, 25), 99, 900, 1)
        };

        var summary = _calculator.Summarise(days, leapToday.AddDays(-60), leapClock);

        summary.ActiveDays.ShouldBe(2);
        summary.TotalMinutes.ShouldBe(60);
        summary.PreviousWindowMinutes.ShouldBe(99);
    }

    [Fact]
    public void A_day_that_somehow_has_no_sessions_is_not_an_active_day()
    {
        var days = new[] { Day(1, minutes: 0, kcal: 0, entryCount: 0) };

        var summary = _calculator.Summarise(days, PlanStart, Clock);

        summary.ActiveDays.ShouldBe(0);
        summary.TotalMinutes.ShouldBe(0);
    }

    private static ExerciseDaySummary Day(int daysAgo, int minutes, int kcal, int entryCount = 1) =>
        new(Today.AddDays(-daysAgo), minutes, kcal, entryCount);
}
