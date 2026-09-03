using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.Domain;

/// <summary>
/// When a member eats, and the timezone arithmetic behind it.
/// </summary>
/// <remarks>
/// The rotation tests are the point of this file. Grouping by UTC hour and shifting by whole hours
/// is the obvious implementation and it is wrong for +05:30, +05:45 and +09:30 — offsets shared by
/// several hundred million people, who would see their dinner reported in the wrong hour. Quarter-
/// hour buckets make it exact, and these prove it.
/// </remarks>
public class PatternAnalyserTests
{
    private readonly PatternAnalyser _analyser = new();

    private static readonly DateOnly Monday = new(2026, 3, 2);

    // --- Time of day ------------------------------------------------------

    [Fact]
    public void With_no_offset_the_buckets_stay_where_they_are()
    {
        var distribution = _analyser.ByHour([Quarter(8, 0, 400), Quarter(21, 0, 600)], utcOffsetMinutes: 0);

        distribution.Shares.Single(s => s.Hour == 8).Kilocalories.ShouldBe(400);
        distribution.Shares.Single(s => s.Hour == 21).Kilocalories.ShouldBe(600);
    }

    [Fact]
    public void A_whole_hour_offset_moves_the_buckets_by_that_many_hours()
    {
        // Logged at 14:00 UTC; in Singapore (+08:00) that is 22:00.
        var distribution = _analyser.ByHour([Quarter(14, 0, 500)], utcOffsetMinutes: 8 * 60);

        distribution.Shares.Single(s => s.Hour == 22).Kilocalories.ShouldBe(500);
        distribution.Shares.Single(s => s.Hour == 14).Kilocalories.ShouldBe(0);
    }

    [Fact]
    public void A_half_hour_offset_lands_exactly_and_not_on_a_whole_hour()
    {
        // 15:45 UTC in India (+05:30) is 21:15 - which a whole-hour rotation would put at either
        // 20:45 or 21:45, in the wrong hour bucket. This is the case the design exists for.
        var distribution = _analyser.ByHour([Quarter(15, 3, 500)], utcOffsetMinutes: (5 * 60) + 30);

        distribution.Shares.Single(s => s.Hour == 21).Kilocalories.ShouldBe(500);
        distribution.Shares.Single(s => s.Hour == 20).Kilocalories.ShouldBe(0);
    }

    [Fact]
    public void A_three_quarter_hour_offset_lands_exactly()
    {
        // 15:30 UTC in Nepal (+05:45) is 21:15.
        var distribution = _analyser.ByHour([Quarter(15, 2, 500)], utcOffsetMinutes: (5 * 60) + 45);

        distribution.Shares.Single(s => s.Hour == 21).Kilocalories.ShouldBe(500);
    }

    [Fact]
    public void A_negative_offset_wraps_backwards_through_midnight()
    {
        // 02:00 UTC in Chicago (-06:00) is 20:00 the previous evening.
        var distribution = _analyser.ByHour([Quarter(2, 0, 300)], utcOffsetMinutes: -6 * 60);

        distribution.Shares.Single(s => s.Hour == 20).Kilocalories.ShouldBe(300);
    }

    [Fact]
    public void A_negative_half_hour_offset_lands_exactly()
    {
        // 12:00 UTC in Newfoundland (-03:30) is 08:30.
        var distribution = _analyser.ByHour([Quarter(12, 0, 300)], utcOffsetMinutes: -((3 * 60) + 30));

        distribution.Shares.Single(s => s.Hour == 8).Kilocalories.ShouldBe(300);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(330)]
    [InlineData(345)]
    [InlineData(-210)]
    [InlineData(720)]
    [InlineData(-720)]
    public void Energy_is_conserved_across_any_rotation(int offsetMinutes)
    {
        // Rotation moves buckets. It must never create or discard a calorie.
        IReadOnlyList<QuarterHourRow> rows =
        [
            Quarter(0, 0, 100), Quarter(7, 2, 250), Quarter(12, 1, 400),
            Quarter(19, 3, 600), Quarter(23, 3, 150)
        ];

        var distribution = _analyser.ByHour(rows, offsetMinutes);

        distribution.TotalKilocalories.ShouldBe(1500);
        distribution.Shares.Count.ShouldBe(24);
    }

    [Fact]
    public void Shares_sum_to_exactly_one_hundred()
    {
        var distribution = _analyser.ByHour(
            [Quarter(8, 0, 1000), Quarter(13, 0, 1000), Quarter(19, 0, 1000)], utcOffsetMinutes: 0);

        distribution.Shares.Sum(s => s.ShareOfTotal).ShouldBe(100m);
    }

    [Fact]
    public void A_period_with_nothing_logged_gives_twenty_four_empty_hours()
    {
        var distribution = _analyser.ByHour([], utcOffsetMinutes: 0);

        distribution.Shares.Count.ShouldBe(24);
        distribution.TotalKilocalories.ShouldBe(0);
        distribution.Shares.ShouldAllBe(s => s.ShareOfTotal == 0m);
    }

    [Fact]
    public void The_approximation_is_stated_on_the_type_rather_than_left_to_a_screen()
    {
        var distribution = _analyser.ByHour([Quarter(9, 0, 100)], utcOffsetMinutes: 0);

        distribution.IsApproximate.ShouldBeTrue();
        distribution.ApproximationReason.ShouldContain("recorded");
        distribution.UtcOffsetMinutes.ShouldBe(0);
    }

    [Fact]
    public void The_late_share_counts_everything_from_a_local_hour_onwards()
    {
        var distribution = _analyser.ByHour(
            [Quarter(8, 0, 700), Quarter(21, 0, 200), Quarter(22, 2, 100)], utcOffsetMinutes: 0);

        distribution.ShareAtOrAfter(21).ShouldBe(30m);
    }

    // --- Day of week ------------------------------------------------------

    [Fact]
    public void Every_weekday_appears_even_when_nothing_was_logged_on_it()
    {
        var distribution = _analyser.ByWeekday([Day(Monday, 2000)]);

        distribution.Shares.Count.ShouldBe(7);
        distribution.Shares.Single(s => s.DayOfWeek == DayOfWeek.Monday).AverageKilocalories.ShouldBe(2000);

        var sunday = distribution.Shares.Single(s => s.DayOfWeek == DayOfWeek.Sunday);
        sunday.AverageKilocalories.ShouldBe(0);
        sunday.LoggedDays.ShouldBe(0);
    }

    [Fact]
    public void A_weekday_reports_an_average_not_a_total()
    {
        // Four Mondays and one Sunday must not make Mondays look four times heavier.
        var distribution = _analyser.ByWeekday(
        [
            Day(Monday, 2000), Day(Monday.AddDays(7), 2200),
            Day(Monday.AddDays(14), 1800), Day(Monday.AddDays(21), 2000),
            Day(Monday.AddDays(6), 2500)
        ]);

        var monday = distribution.Shares.Single(s => s.DayOfWeek == DayOfWeek.Monday);
        monday.AverageKilocalories.ShouldBe(2000);
        monday.LoggedDays.ShouldBe(4);

        distribution.Shares.Single(s => s.DayOfWeek == DayOfWeek.Sunday).AverageKilocalories.ShouldBe(2500);
    }

    [Fact]
    public void The_weekend_average_is_weighted_by_how_many_of_each_day_were_logged()
    {
        // Two Saturdays at 3,000 and one Sunday at 1,500 average 2,500 - not 2,250, which is what
        // averaging the two averages would give.
        var distribution = _analyser.ByWeekday(
        [
            Day(Monday.AddDays(5), 3000), Day(Monday.AddDays(12), 3000),
            Day(Monday.AddDays(6), 1500)
        ]);

        distribution.WeekendAverage.ShouldBe(2500);
        distribution.LoggedDaysOn(DayOfWeek.Saturday, DayOfWeek.Sunday).ShouldBe(3);
    }

    [Fact]
    public void The_weekend_and_weekday_averages_are_null_when_nothing_was_logged_on_them()
    {
        var weekdaysOnly = _analyser.ByWeekday([Day(Monday, 2000)]);

        weekdaysOnly.WeekendAverage.ShouldBeNull();
        weekdaysOnly.WeekdayAverage.ShouldBe(2000);

        _analyser.ByWeekday([]).WeekdayAverage.ShouldBeNull();
    }

    // --- Helpers ----------------------------------------------------------

    private static QuarterHourRow Quarter(int hour, int quarter, int kcal) => new(hour, quarter, kcal);

    private static DayIntakeRow Day(DateOnly date, int calories) =>
        new(date, calories, 100m, 200m, 70m, 2100, 157.5m, 210m, 70m);
}
