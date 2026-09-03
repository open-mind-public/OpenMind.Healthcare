using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.Domain;

/// <summary>
/// Laying logged days across a calendar, so a chart can draw the gaps.
/// </summary>
/// <remarks>
/// Two ways a line chart lies about a food log, and both are prevented here. Omitting unlogged
/// days compresses time, so a fortnight of neglect looks like a continuous run. Filling them with
/// zero draws intake that never happened. Every calendar day gets a point, and the unlogged ones
/// are flagged so the line breaks instead.
/// </remarks>
public class TrendAnalyserTests
{
    private readonly TrendAnalyser _analyser = new();
    private readonly AnalysisPeriodResolver _resolver = new();

    private static readonly DateTime Clock = new(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Clock);

    [Fact]
    public void Every_calendar_day_in_the_period_gets_a_point()
    {
        var period = Week();

        var trend = _analyser.Build(period, [Row(Today, 2000)]);

        trend.Points.Count.ShouldBe(7);
        trend.Points.Select(p => p.Date).ShouldBe(Enumerable.Range(0, 7).Select(i => period.From.AddDays(i)));
    }

    [Fact]
    public void Days_the_member_did_not_log_are_flagged_rather_than_omitted()
    {
        var period = Week();

        var trend = _analyser.Build(period, [Row(Today, 2000), Row(Today.AddDays(-3), 1800)]);

        trend.LoggedDays.ShouldBe(2);
        trend.Points.Count(p => p.Logged).ShouldBe(2);
        trend.Points.Count(p => !p.Logged).ShouldBe(5);
    }

    [Fact]
    public void An_unlogged_day_carries_no_intake_to_plot()
    {
        var period = Week();

        var trend = _analyser.Build(period, [Row(Today, 2000)]);

        var gap = trend.Points.First(p => !p.Logged);
        gap.Logged.ShouldBeFalse();
        gap.Calories.ShouldBe(0);
    }

    [Fact]
    public void The_target_is_carried_across_unlogged_days()
    {
        // A member's target is in force whether or not they log. Breaking the reference line at a
        // gap would suggest they had no target that day, which is not true.
        var period = Week();

        var trend = _analyser.Build(period, [Row(period.From, 2000, target: 2100)]);

        trend.Points.ShouldAllBe(p => p.TargetCalories == 2100);
    }

    [Fact]
    public void The_target_changes_from_the_day_it_changed_and_not_before()
    {
        var period = Week();

        var trend = _analyser.Build(period,
        [
            Row(period.From, 2000, target: 2400),
            Row(period.From.AddDays(4), 2000, target: 1900)
        ]);

        // Days before the change keep the old target; the change applies from its own day onward.
        trend.Points.Take(4).ShouldAllBe(p => p.TargetCalories == 2400);
        trend.Points.Skip(4).ShouldAllBe(p => p.TargetCalories == 1900);
    }

    [Fact]
    public void A_period_before_the_first_logged_day_uses_the_earliest_known_target()
    {
        // There is no stored target before the first logged day, so the earliest one is the only
        // honest thing to show - rather than a zero, which would draw the axis through the floor.
        var period = Week();

        var trend = _analyser.Build(period, [Row(period.To, 2000, target: 1850)]);

        trend.Points.First().TargetCalories.ShouldBe(1850);
    }

    [Fact]
    public void A_member_with_nothing_logged_still_gets_a_full_row_of_gaps()
    {
        var period = Week();

        var trend = _analyser.Build(period, []);

        trend.Points.Count.ShouldBe(7);
        trend.LoggedDays.ShouldBe(0);
        trend.Points.ShouldAllBe(p => !p.Logged);
        trend.PeakCalories.ShouldBe(0);
    }

    [Fact]
    public void The_peak_covers_both_the_intake_and_the_target_so_one_axis_fits_both()
    {
        var period = Week();

        _analyser.Build(period, [Row(Today, 2600, target: 2100)]).PeakCalories.ShouldBe(2600);
        _analyser.Build(period, [Row(Today, 1500, target: 2100)]).PeakCalories.ShouldBe(2100);
    }

    [Fact]
    public void Points_come_back_in_date_order()
    {
        var period = Week();

        var trend = _analyser.Build(period,
        [
            Row(Today, 2000),
            Row(Today.AddDays(-5), 1800),
            Row(Today.AddDays(-2), 2200)
        ]);

        trend.Points.Select(p => p.Date).ShouldBeInOrder();
    }

    [Fact]
    public void Macronutrients_ride_along_on_the_logged_points()
    {
        var period = Week();

        var trend = _analyser.Build(period, [Row(Today, 2000, protein: 140m)]);

        var logged = trend.Points.Single(p => p.Logged);
        logged.ProteinG.ShouldBe(140m);
        trend.Points.Where(p => !p.Logged).ShouldAllBe(p => p.ProteinG == 0m);
    }

    private AnalysisPeriod Week() =>
        _resolver.Resolve(PeriodPreset.Week, Today.AddDays(-365), Clock);

    private static DayIntakeRow Row(
        DateOnly date, int calories, int target = 2100, decimal protein = 100m) =>
        new(date, calories, protein, 200m, 70m, target, 157.5m, 210m, 70m);
}
