using QuitSmokingApi.Domain.Entities;
using QuitSmokingApi.Domain.ValueObjects;
using QuitSmokingApi.Tests.TestSupport;

namespace QuitSmokingApi.Tests.Domain;

public class RelapseAnalyticsTests
{
    [Fact]
    public void A_journey_with_nothing_marked_reports_a_clean_record()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(70).Smoking(20, 20, 10m);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock);

        analytics.SmokedDays.ShouldBe(0);
        analytics.SmokeFreeDays.ShouldBe(70);
        analytics.SmokeFreeRate.ShouldBe(100);
        analytics.RelapseRate.ShouldBe(0);
        analytics.TotalCigarettesSmoked.ShouldBe(0);
        analytics.MoneySpentOnRelapses.Amount.ShouldBe(0m);
        analytics.LastRelapseDate.ShouldBeNull();
        analytics.FirstRelapseDate.ShouldBeNull();
        analytics.DaysSinceLastRelapse.ShouldBe(0);
        analytics.MostCommonTrigger.ShouldBeNull();
        analytics.RiskiestWeekday.ShouldBeNull();
        analytics.TriggerBreakdown.ShouldBeEmpty();
        analytics.AverageCigarettesPerRelapseDay.ShouldBe(0);
        analytics.AverageDaysBetweenRelapses.ShouldBe(0);
    }

    [Fact]
    public void The_headline_numbers_match_the_days_that_were_marked()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(70)
            .Smoking(20, 20, 10m)
            .SmokedDaysAgo(40, cigarettes: 8)
            .SmokedDaysAgo(12, cigarettes: 3)
            .SmokedDaysAgo(5, cigarettes: 9);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock);

        analytics.TotalDaysInJourney.ShouldBe(70);
        analytics.SmokedDays.ShouldBe(3);
        analytics.SmokeFreeDays.ShouldBe(67);
        analytics.SmokeFreeRate.ShouldBe(95.71);
        analytics.RelapseRate.ShouldBe(4.29);
        analytics.TotalCigarettesSmoked.ShouldBe(20);
        analytics.MoneySpentOnRelapses.Amount.ShouldBe(10m);
        analytics.MoneySaved.Amount.ShouldBe(670m);
        analytics.LifeLostToRelapses.TotalMinutes.ShouldBe(220);
        analytics.CurrentStreak.ShouldBe(5);
        analytics.LongestStreak.ShouldBe(30);
    }

    [Fact]
    public void The_first_and_last_slip_are_reported_with_how_long_ago_the_last_one_was()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(70)
            .SmokedDaysAgo(40)
            .SmokedDaysAgo(12)
            .SmokedDaysAgo(5);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock);

        analytics.FirstRelapseDate.ShouldBe(builder.DaysAgo(40));
        analytics.LastRelapseDate.ShouldBe(builder.DaysAgo(5));
        analytics.DaysSinceLastRelapse.ShouldBe(5);
    }

    [Fact]
    public void Averages_describe_how_heavy_and_how_frequent_the_slips_are()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(70)
            .SmokedDaysAgo(40, cigarettes: 8)
            .SmokedDaysAgo(12, cigarettes: 3)
            .SmokedDaysAgo(5, cigarettes: 9);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock);

        analytics.AverageCigarettesPerRelapseDay.ShouldBe(6.7); // 20 over 3 days
        analytics.AverageDaysBetweenRelapses.ShouldBe(23.7);    // 71 calendar days over 3 slips
    }

    [Fact]
    public void Triggers_are_ranked_by_how_many_days_they_account_for()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(90)
            .SmokedDaysAgo(70, cigarettes: 6, trigger: RelapseTrigger.Stress)
            .SmokedDaysAgo(50, cigarettes: 4, trigger: RelapseTrigger.Stress)
            .SmokedDaysAgo(20, cigarettes: 5, trigger: RelapseTrigger.Stress)
            .SmokedDaysAgo(10, cigarettes: 9, trigger: RelapseTrigger.Alcohol);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock);

        analytics.TriggerBreakdown.Select(t => t.Trigger)
            .ShouldBe([RelapseTrigger.Stress, RelapseTrigger.Alcohol]);

        var stress = analytics.TriggerBreakdown[0];
        stress.Days.ShouldBe(3);
        stress.Cigarettes.ShouldBe(15);
        stress.SharePercentage.ShouldBe(75);

        var alcohol = analytics.TriggerBreakdown[1];
        alcohol.Days.ShouldBe(1);
        alcohol.Cigarettes.ShouldBe(9);
        alcohol.SharePercentage.ShouldBe(25);

        analytics.MostCommonTrigger.ShouldBe(RelapseTrigger.Stress);
    }

    [Fact]
    public void Days_marked_without_naming_a_trigger_are_grouped_under_unspecified()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30).SmokedDaysAgo(3).SmokedDaysAgo(8);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock);

        analytics.TriggerBreakdown.ShouldHaveSingleItem();
        analytics.TriggerBreakdown[0].Trigger.ShouldBe(RelapseTrigger.Unspecified);
        analytics.TriggerBreakdown[0].Days.ShouldBe(2);
    }

    [Fact]
    public void Every_weekday_is_reported_and_together_they_cover_the_whole_journey()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(70).SmokedDaysAgo(40).SmokedDaysAgo(5);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock);

        analytics.WeekdayBreakdown.Select(w => w.Weekday)
            .ShouldBe(Enum.GetValues<DayOfWeek>());
        analytics.WeekdayBreakdown.Sum(w => w.TotalDays).ShouldBe(71); // quit day through today
        analytics.WeekdayBreakdown.Sum(w => w.SmokedDays).ShouldBe(2);
    }

    [Fact]
    public void The_riskiest_weekday_is_the_one_the_slips_keep_landing_on()
    {
        // Whole weeks back from today all land on today's weekday
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(70)
            .SmokedDaysAgo(7)
            .SmokedDaysAgo(14)
            .SmokedDaysAgo(21);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock);

        analytics.RiskiestWeekday.ShouldBe(builder.Today.DayOfWeek);

        var riskiest = analytics.WeekdayBreakdown.Single(w => w.Weekday == builder.Today.DayOfWeek);
        riskiest.SmokedDays.ShouldBe(3);
        riskiest.RelapseRate.ShouldBeGreaterThan(0);
        riskiest.Name.ShouldBe(builder.Today.DayOfWeek.ToString());
    }

    [Fact]
    public void A_weekday_that_was_never_smoked_on_reports_a_zero_rate()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(70).SmokedDaysAgo(7);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock);

        var untouched = analytics.WeekdayBreakdown.Single(w => w.Weekday == builder.Today.AddDays(-1).DayOfWeek);
        untouched.SmokedDays.ShouldBe(0);
        untouched.RelapseRate.ShouldBe(0);
        untouched.TotalDays.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Each_month_splits_its_days_into_smoke_free_and_smoked()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(70).SmokedDaysAgo(40).SmokedDaysAgo(5);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock);

        analytics.MonthlyBreakdown.ShouldAllBe(m => m.SmokedDays + m.SmokeFreeDays == m.TotalDays);
        analytics.MonthlyBreakdown.Sum(m => m.TotalDays).ShouldBe(71);
        analytics.MonthlyBreakdown.Sum(m => m.SmokedDays).ShouldBe(2);
        analytics.MonthlyBreakdown[^1].Year.ShouldBe(builder.Today.Year);
        analytics.MonthlyBreakdown[^1].Month.ShouldBe(builder.Today.Month);
    }

    [Fact]
    public void Months_are_listed_oldest_first()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(200);

        var months = builder.Build().GetRelapseAnalytics(builder.Clock).MonthlyBreakdown;

        months.Select(m => m.Year * 100 + m.Month).ShouldBeInOrder();
    }

    [Fact]
    public void A_long_journey_only_reports_the_last_twelve_months()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(800);

        var months = builder.Build().GetRelapseAnalytics(builder.Clock).MonthlyBreakdown;

        months.Count.ShouldBe(12);
        months[^1].Month.ShouldBe(builder.Today.Month);
    }

    [Fact]
    public void A_month_with_no_slips_reads_as_fully_smoke_free()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(70);

        var months = builder.Build().GetRelapseAnalytics(builder.Clock).MonthlyBreakdown;

        months.ShouldAllBe(m => m.SmokeFreeRate == 100);
        months.ShouldAllBe(m => m.SmokedDays == 0);
    }

    [Fact]
    public void A_journey_too_short_to_compare_two_months_reports_no_trend_yet()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(59).SmokedDaysAgo(3);

        builder.Build().GetRelapseAnalytics(builder.Clock).Trend.ShouldBe(RelapseTrend.NotEnoughData);
    }

    [Fact]
    public void Two_full_months_in_the_trend_can_be_compared()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(60);

        builder.Build().GetRelapseAnalytics(builder.Clock).Trend.ShouldBe(RelapseTrend.Stable);
    }

    [Fact]
    public void Fewer_slips_than_the_month_before_reads_as_improving()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(90)
            .SmokedDaysAgo(45)
            .SmokedDaysAgo(40)
            .SmokedDaysAgo(35)
            .SmokedDaysAgo(10);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock);

        analytics.RelapsesPrevious30Days.ShouldBe(3);
        analytics.RelapsesLast30Days.ShouldBe(1);
        analytics.Trend.ShouldBe(RelapseTrend.Improving);
    }

    [Fact]
    public void More_slips_than_the_month_before_reads_as_worsening()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(90)
            .SmokedDaysAgo(40)
            .SmokedDaysAgo(20)
            .SmokedDaysAgo(10)
            .SmokedDaysAgo(2);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock);

        analytics.RelapsesPrevious30Days.ShouldBe(1);
        analytics.RelapsesLast30Days.ShouldBe(3);
        analytics.Trend.ShouldBe(RelapseTrend.Worsening);
    }

    [Fact]
    public void The_same_number_of_slips_two_months_running_reads_as_holding_steady()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(90)
            .SmokedDaysAgo(40)
            .SmokedDaysAgo(10);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock);

        analytics.RelapsesPrevious30Days.ShouldBe(1);
        analytics.RelapsesLast30Days.ShouldBe(1);
        analytics.Trend.ShouldBe(RelapseTrend.Stable);
    }

    [Fact]
    public void There_is_nothing_to_analyse_before_the_journey_started()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(70)
            .Smoking(20, 20, 40000m, "VND")
            .SmokedDaysAgo(40);

        var analytics = builder.Build().GetRelapseAnalytics(builder.Clock.AddDays(-71));

        analytics.SmokedDays.ShouldBe(0);
        analytics.TotalDaysInJourney.ShouldBe(0);
        analytics.Trend.ShouldBe(RelapseTrend.NotEnoughData);
        analytics.MoneySpentOnRelapses.Currency.ShouldBe("VND");
    }
}
