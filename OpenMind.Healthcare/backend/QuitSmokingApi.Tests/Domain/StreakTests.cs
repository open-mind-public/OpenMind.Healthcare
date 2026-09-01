using QuitSmokingApi.Tests.TestSupport;

namespace QuitSmokingApi.Tests.Domain;

public class StreakTests
{
    [Fact]
    public void An_unbroken_journey_is_one_long_streak()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(70);

        var stats = builder.Build().GetStatistics(builder.Clock);

        stats.CurrentStreak.ShouldBe(70);
        stats.LongestStreak.ShouldBe(70);
    }

    [Fact]
    public void Smoking_today_resets_the_current_streak_to_nothing()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(70);
        var journey = builder.Build();

        journey.MarkDayAsSmoked(builder.Today, 4, asOf: builder.Clock);

        journey.GetCurrentStreak(builder.Clock).ShouldBe(0);
    }

    [Fact]
    public void The_current_streak_counts_only_the_days_since_the_last_marked_day()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(70).SmokedDaysAgo(5);

        builder.Build().GetCurrentStreak(builder.Clock).ShouldBe(5);
    }

    [Fact]
    public void The_longest_streak_is_the_longest_run_between_marked_days()
    {
        // Runs of 30, 27, 6 and 5 days sit between marks at 40, 12 and 5 days ago
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(70)
            .SmokedDaysAgo(40)
            .SmokedDaysAgo(12)
            .SmokedDaysAgo(5);

        var journey = builder.Build();

        journey.GetLongestStreak(builder.Clock).ShouldBe(30);
        journey.GetCurrentStreak(builder.Clock).ShouldBe(5);
    }

    [Fact]
    public void A_new_slip_resets_the_current_streak_but_leaves_the_best_one_standing()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(70).SmokedDaysAgo(40);
        var journey = builder.Build();

        journey.GetCurrentStreak(builder.Clock).ShouldBe(40);
        journey.GetLongestStreak(builder.Clock).ShouldBe(40);

        journey.MarkDayAsSmoked(builder.Today, 3, asOf: builder.Clock);

        journey.GetCurrentStreak(builder.Clock).ShouldBe(0);
        journey.GetLongestStreak(builder.Clock).ShouldBe(39);
    }

    [Fact]
    public void Marking_the_quit_day_itself_still_leaves_the_rest_of_the_journey_as_a_streak()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(10);
        var journey = builder.Build();

        journey.MarkDayAsSmoked(journey.QuitDay, 3, asOf: builder.Clock);

        journey.GetCurrentStreak(builder.Clock).ShouldBe(10);
        journey.GetLongestStreak(builder.Clock).ShouldBe(10);
    }

    [Fact]
    public void A_streak_is_never_reported_as_longer_than_the_journey_itself()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(3);

        var stats = builder.Build().GetStatistics(builder.Clock);

        stats.CurrentStreak.ShouldBe(stats.TotalDaysInJourney);
        stats.LongestStreak.ShouldBe(stats.TotalDaysInJourney);
    }

    [Fact]
    public void A_journey_that_has_not_started_yet_has_no_streak()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(10);
        var journey = builder.Build();

        journey.GetCurrentStreak(builder.Clock.AddDays(-11)).ShouldBe(0);
        journey.GetLongestStreak(builder.Clock.AddDays(-11)).ShouldBe(0);
    }

    [Fact]
    public void Unmarking_a_day_joins_the_streaks_either_side_of_it_back_together()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(70).SmokedDaysAgo(35);
        var journey = builder.Build();

        journey.GetCurrentStreak(builder.Clock).ShouldBe(35);

        journey.UnmarkSmokedDay(builder.DaysAgo(35));

        journey.GetCurrentStreak(builder.Clock).ShouldBe(70);
        journey.GetLongestStreak(builder.Clock).ShouldBe(70);
    }
}
