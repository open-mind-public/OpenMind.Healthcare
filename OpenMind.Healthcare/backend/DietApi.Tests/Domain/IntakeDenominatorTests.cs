using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.Domain;

/// <summary>
/// The two denominators, and why a figure without one is a lie.
/// </summary>
/// <remarks>
/// A member who logged three days of a thirty-day month has an average over three days. Reporting
/// it beside the word "month" without saying so is the single easiest way for an analytics feature
/// to mislead, and it is what FR-003 and SC-003 exist to prevent.
/// </remarks>
public class IntakeDenominatorTests
{
    private readonly IntakeAnalyser _analyser = new();

    private static readonly DateOnly Start = new(2026, 3, 1);

    [Fact]
    public void The_intake_average_divides_by_logged_days_not_calendar_days()
    {
        // Three days at 2,100 in a thirty day month. The average is 2,100 - not 210.
        var summary = _analyser.Summarise(
        [
            IntakeAnalyserTests.Row(Start, 2100, 2100),
            IntakeAnalyserTests.Row(Start.AddDays(1), 2100, 2100),
            IntakeAnalyserTests.Row(Start.AddDays(2), 2100, 2100)
        ], totalDays: 30);

        summary.AverageDailyKilocalories.ShouldBe(2100);
        summary.TotalKilocalories.ShouldBe(6300);
    }

    [Fact]
    public void The_average_carries_the_number_of_days_it_divided_by()
    {
        var summary = _analyser.Summarise(
        [
            IntakeAnalyserTests.Row(Start, 2100, 2100),
            IntakeAnalyserTests.Row(Start.AddDays(1), 1900, 2100),
            IntakeAnalyserTests.Row(Start.AddDays(2), 2000, 2100)
        ], totalDays: 30);

        summary.AveragedOverDays.ShouldBe(3);
        summary.AveragedOver.ShouldBe(AveragedOver.LoggedDays);
    }

    [Fact]
    public void The_day_state_split_counts_every_calendar_day_in_the_period()
    {
        var summary = _analyser.Summarise(
        [
            IntakeAnalyserTests.Row(Start, 1800, 2100),
            IntakeAnalyserTests.Row(Start.AddDays(1), 2500, 2100)
        ], totalDays: 30);

        summary.OnTargetDays.ShouldBe(1);
        summary.OverTargetDays.ShouldBe(1);
        summary.NotLoggedDays.ShouldBe(28);
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(1, 7)]
    [InlineData(24, 30)]
    [InlineData(90, 90)]
    public void The_three_day_states_always_sum_to_the_period(int loggedDays, int totalDays)
    {
        var days = Enumerable.Range(0, loggedDays)
            .Select(i => IntakeAnalyserTests.Row(Start.AddDays(i), i % 2 == 0 ? 1800 : 2500, 2100))
            .ToList();

        var summary = _analyser.Summarise(days, totalDays);

        (summary.OnTargetDays + summary.OverTargetDays + summary.NotLoggedDays).ShouldBe(totalDays);
    }

    [Fact]
    public void A_member_who_logged_nothing_gets_zeros_and_a_full_count_of_missed_days()
    {
        var summary = _analyser.Summarise([], totalDays: 30);

        summary.TotalKilocalories.ShouldBe(0);
        summary.AverageDailyKilocalories.ShouldBe(0);
        summary.AveragedOverDays.ShouldBe(0);
        summary.NotLoggedDays.ShouldBe(30);
    }

    [Fact]
    public void An_average_over_no_days_is_zero_rather_than_a_division_by_zero()
    {
        Should.NotThrow(() => _analyser.Summarise([], totalDays: 0));
        IntakeSummary.Empty(0).AverageDailyKilocalories.ShouldBe(0);
    }
}
