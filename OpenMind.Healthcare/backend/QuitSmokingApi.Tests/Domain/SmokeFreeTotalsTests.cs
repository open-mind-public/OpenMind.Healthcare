using QuitSmokingApi.Domain.Entities;
using QuitSmokingApi.Tests.TestSupport;

namespace QuitSmokingApi.Tests.Domain;

/// <summary>
/// The headline behaviour of the feature: a day the user marked as smoked stops counting towards
/// every smoke-free total. 70 days at 20 a day, 20 to a $10 pack, is 50c a cigarette throughout.
/// </summary>
public class SmokeFreeTotalsTests
{
    private static JourneyBuilder SeventyDaysAtTwentyADay() =>
        JourneyBuilder.AJourney().StartedDaysAgo(70).Smoking(cigarettesPerDay: 20, cigarettesPerPack: 20, pricePerPack: 10m);

    [Fact]
    public void With_no_marked_days_every_elapsed_day_counts_as_smoke_free()
    {
        var builder = SeventyDaysAtTwentyADay();
        var stats = builder.Build().GetStatistics(builder.Clock);

        stats.TotalDaysInJourney.ShouldBe(70);
        stats.SmokedDays.ShouldBe(0);
        stats.DaysSmokeFree.ShouldBe(70);
        stats.CigarettesAvoided.ShouldBe(1400);
        stats.MoneySaved.Amount.ShouldBe(700m);
        stats.CigarettesSmoked.ShouldBe(0);
        stats.MoneySpentOnRelapses.Amount.ShouldBe(0m);
        stats.SmokeFreeRate.ShouldBe(100);
    }

    [Fact]
    public void Marked_days_are_taken_off_the_smoke_free_day_count()
    {
        var builder = SeventyDaysAtTwentyADay()
            .SmokedDaysAgo(40)
            .SmokedDaysAgo(12)
            .SmokedDaysAgo(5);

        var stats = builder.Build().GetStatistics(builder.Clock);

        stats.TotalDaysInJourney.ShouldBe(70);
        stats.SmokedDays.ShouldBe(3);
        stats.DaysSmokeFree.ShouldBe(67);
    }

    [Fact]
    public void Marked_days_are_taken_off_the_cigarettes_money_and_life_totals()
    {
        var builder = SeventyDaysAtTwentyADay()
            .SmokedDaysAgo(40)
            .SmokedDaysAgo(12)
            .SmokedDaysAgo(5);

        var stats = builder.Build().GetStatistics(builder.Clock);

        stats.CigarettesAvoided.ShouldBe(1340);          // 67 smoke-free days x 20
        stats.MoneySaved.Amount.ShouldBe(670m);          // 1340 x 50c
        stats.LifeRegained.TotalMinutes.ShouldBe(14740); // 1340 x 11 minutes
    }

    [Fact]
    public void The_cigarettes_smoked_on_marked_days_are_totalled_and_priced()
    {
        var builder = SeventyDaysAtTwentyADay()
            .SmokedDaysAgo(40, cigarettes: 8)
            .SmokedDaysAgo(12, cigarettes: 3)
            .SmokedDaysAgo(5, cigarettes: 9);

        var journey = builder.Build();

        journey.GetCigarettesSmoked(builder.Clock).ShouldBe(20);
        journey.GetMoneySpentOnRelapses(builder.Clock).Amount.ShouldBe(10m);   // 20 x 50c
        journey.GetLifeLostToRelapses(builder.Clock).TotalMinutes.ShouldBe(220); // 20 x 11 minutes
    }

    [Fact]
    public void Money_spent_on_relapses_is_reported_in_the_journey_currency()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(10)
            .Smoking(20, 20, pricePerPack: 40000m, currency: "VND")
            .SmokedDaysAgo(2, cigarettes: 4);

        var spent = builder.Build().GetMoneySpentOnRelapses(builder.Clock);

        spent.Currency.ShouldBe("VND");
        spent.Amount.ShouldBe(8000m); // 4 x 2000
    }

    [Fact]
    public void Smoke_free_time_drops_by_a_whole_day_for_each_marked_day()
    {
        var builder = SeventyDaysAtTwentyADay().SmokedDaysAgo(30).SmokedDaysAgo(3);

        var journey = builder.Build();

        journey.GetTimeSinceQuit(builder.Clock).TotalMinutes.ShouldBe(70 * 24 * 60);
        journey.GetTimeSmokeFree(builder.Clock).TotalMinutes.ShouldBe(68 * 24 * 60);
    }

    [Fact]
    public void Totals_never_go_negative_when_almost_every_day_was_smoked()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(2).Smoking(20, 20, 10m);
        var journey = builder.Build();

        // Three calendar days sit in a two-day-old journey: the quit day, yesterday and today
        journey.MarkDayAsSmoked(builder.DaysAgo(2), 5, asOf: builder.Clock);
        journey.MarkDayAsSmoked(builder.DaysAgo(1), 5, asOf: builder.Clock);
        journey.MarkDayAsSmoked(builder.Today, 5, asOf: builder.Clock);

        var stats = journey.GetStatistics(builder.Clock);

        stats.DaysSmokeFree.ShouldBe(0);
        stats.CigarettesAvoided.ShouldBe(0);
        stats.MoneySaved.Amount.ShouldBe(0m);
        stats.LifeRegained.TotalMinutes.ShouldBe(0);
        stats.SmokeFreeRate.ShouldBe(0);
    }

    [Fact]
    public void A_day_marked_after_the_moment_being_reported_on_is_not_counted_yet()
    {
        var builder = SeventyDaysAtTwentyADay();
        var journey = builder.Build();
        journey.MarkDayAsSmoked(builder.Today, 5, asOf: builder.Clock);

        var yesterdaysView = journey.GetStatistics(builder.Clock.AddDays(-1));

        yesterdaysView.TotalDaysInJourney.ShouldBe(69);
        yesterdaysView.SmokedDays.ShouldBe(0);
        yesterdaysView.DaysSmokeFree.ShouldBe(69);
    }

    [Fact]
    public void Nothing_has_happened_yet_when_reporting_from_before_the_quit_date()
    {
        var builder = SeventyDaysAtTwentyADay().SmokedDaysAgo(40);

        var stats = builder.Build().GetStatistics(builder.Clock.AddDays(-71));

        stats.TotalDaysInJourney.ShouldBe(0);
        stats.DaysSmokeFree.ShouldBe(0);
        stats.SmokedDays.ShouldBe(0);
        stats.CurrentStreak.ShouldBe(0);
    }

    [Fact]
    public void The_milestone_reflects_the_reduced_smoke_free_day_count()
    {
        var withoutRelapses = SeventyDaysAtTwentyADay();
        var withRelapses = SeventyDaysAtTwentyADay()
            .SmokedDaysAgo(40)
            .SmokedDaysAgo(12)
            .SmokedDaysAgo(5);

        var clean = withoutRelapses.Build().GetStatistics(withoutRelapses.Clock);
        var lapsed = withRelapses.Build().GetStatistics(withRelapses.Clock);

        clean.CurrentMilestone.RequiredDays.ShouldBe(60);
        clean.NextMilestone!.RequiredDays.ShouldBe(90);
        clean.DaysToNextMilestone.ShouldBe(20);

        lapsed.CurrentMilestone.RequiredDays.ShouldBe(60);
        lapsed.DaysToNextMilestone.ShouldBe(23); // three days further away than the clean journey
    }

    [Fact]
    public void Enough_marked_days_can_push_the_journey_back_below_a_milestone()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(31).Smoking(20, 20, 10m);
        var journey = builder.Build();
        foreach (var daysAgo in new[] { 2, 3 })
        {
            journey.MarkDayAsSmoked(builder.DaysAgo(daysAgo), 5, asOf: builder.Clock);
        }

        var stats = journey.GetStatistics(builder.Clock);

        stats.DaysSmokeFree.ShouldBe(29);
        stats.CurrentMilestone.RequiredDays.ShouldBe(21); // no longer the one-month milestone
        stats.NextMilestone!.RequiredDays.ShouldBe(30);
    }

    [Fact]
    public void Progress_towards_a_year_is_measured_in_smoke_free_days()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(365).Smoking(20, 20, 10m).SmokedDaysAgo(9);

        var stats = builder.Build().GetStatistics(builder.Clock);

        stats.DaysSmokeFree.ShouldBe(364);
        stats.ProgressPercentage.ShouldBe(99.73);
    }

    [Fact]
    public void Progress_is_capped_at_a_hundred_percent()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(500).Smoking(20, 20, 10m);

        builder.Build().GetStatistics(builder.Clock).ProgressPercentage.ShouldBe(100);
    }

    [Fact]
    public void Correcting_a_mistaken_mark_restores_the_totals()
    {
        var builder = SeventyDaysAtTwentyADay().SmokedDaysAgo(5, cigarettes: 6, trigger: RelapseTrigger.Stress);
        var journey = builder.Build();

        journey.GetStatistics(builder.Clock).DaysSmokeFree.ShouldBe(69);

        journey.UnmarkSmokedDay(builder.DaysAgo(5));

        var stats = journey.GetStatistics(builder.Clock);
        stats.DaysSmokeFree.ShouldBe(70);
        stats.CigarettesAvoided.ShouldBe(1400);
        stats.MoneySaved.Amount.ShouldBe(700m);
        stats.CigarettesSmoked.ShouldBe(0);
    }

    [Fact]
    public void Only_full_elapsed_days_count_so_a_part_day_is_not_yet_smoke_free()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(5)
            .ShiftedBy(TimeSpan.FromHours(6)) // quit six hours into the first day
            .Smoking(20, 20, 10m);

        var stats = builder.Build().GetStatistics(builder.Clock);

        stats.TotalDaysInJourney.ShouldBe(4);
        stats.DaysSmokeFree.ShouldBe(4);
        stats.CigarettesAvoided.ShouldBe(95); // 4.75 days x 20
    }
}
