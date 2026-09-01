using QuitSmokingApi.Domain.Aggregates;
using QuitSmokingApi.Domain.Services;
using QuitSmokingApi.Domain.ValueObjects;
using QuitSmokingApi.Tests.TestSupport;

namespace QuitSmokingApi.Tests.Domain;

public class AchievementStatusServiceTests
{
    private readonly AchievementStatusService _service = new();

    private static Achievement OneWeekAchievement() =>
        Achievement.Create("One Week", "Seven days down", "🏅", 7, AchievementCategory.Milestone);

    [Fact]
    public void Someone_with_no_journey_yet_has_not_started_any_achievement()
    {
        var status = _service.ComputeStatus(OneWeekAchievement(), journey: null);

        status.IsUnlocked.ShouldBeFalse();
        status.ProgressPercentage.ShouldBe(0);
        status.UnlockedAt.ShouldBeNull();
        status.Name.ShouldBe("One Week");
    }

    [Fact]
    public void An_achievement_unlocks_once_the_smoke_free_days_reach_it()
    {
        var journey = JourneyBuilder.AJourney().StartedDaysAgo(8).Build();

        var status = _service.ComputeStatus(OneWeekAchievement(), journey);

        status.IsUnlocked.ShouldBeTrue();
        status.ProgressPercentage.ShouldBe(100);
        status.UnlockedAt.ShouldBe(journey.QuitDate.AddDays(7));
    }

    [Fact]
    public void Days_marked_as_smoked_hold_an_achievement_back()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(8).SmokedDaysAgo(3).SmokedDaysAgo(5);

        var status = _service.ComputeStatus(OneWeekAchievement(), builder.Build());

        status.IsUnlocked.ShouldBeFalse();                          // six smoke-free days, not seven
        status.ProgressPercentage.ShouldBe(Math.Round(6 / 7d * 100, 2));
    }

    [Fact]
    public void Statuses_are_computed_for_every_achievement_given()
    {
        var journey = JourneyBuilder.AJourney().StartedDaysAgo(10).Build();
        var achievements = new[]
        {
            Achievement.Create("Day one", "", "🌟", 1, AchievementCategory.Milestone),
            Achievement.Create("One week", "", "🏅", 7, AchievementCategory.Milestone),
            Achievement.Create("One month", "", "👑", 30, AchievementCategory.Milestone)
        };

        var statuses = _service.ComputeStatuses(achievements, journey);

        statuses.Select(s => s.IsUnlocked).ShouldBe([true, true, false]);
    }
}

public class HealthMilestoneStatusServiceTests
{
    private readonly HealthMilestoneStatusService _service = new();

    private static HealthMilestone TwoDayMilestone() =>
        HealthMilestone.Create("Two days", "Smell and taste return", 2 * 24 * 60, "2 days", "👃", HealthCategory.Sensory);

    [Fact]
    public void Someone_with_no_journey_yet_has_not_started_healing()
    {
        var status = _service.ComputeStatus(TwoDayMilestone(), journey: null);

        status.IsAchieved.ShouldBeFalse();
        status.ProgressPercentage.ShouldBe(0);
        status.AchievedAt.ShouldBeNull();
    }

    [Fact]
    public void A_milestone_is_reached_once_enough_smoke_free_time_has_passed()
    {
        var journey = JourneyBuilder.AJourney().StartedDaysAgo(2).Build();

        var status = _service.ComputeStatus(TwoDayMilestone(), journey);

        status.IsAchieved.ShouldBeTrue();
        status.ProgressPercentage.ShouldBe(100);
    }

    [Fact]
    public void Healing_is_measured_in_smoke_free_time_so_a_marked_day_undoes_a_milestone()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(2).SmokedDaysAgo(1);

        var status = _service.ComputeStatus(TwoDayMilestone(), builder.Build());

        status.IsAchieved.ShouldBeFalse();
        status.ProgressPercentage.ShouldBe(50); // one smoke-free day out of the two needed
    }

    [Fact]
    public void A_marked_day_pushes_the_date_a_milestone_is_reached_back_by_a_day()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(10).SmokedDaysAgo(4);
        var journey = builder.Build();

        var status = _service.ComputeStatus(TwoDayMilestone(), journey);

        status.IsAchieved.ShouldBeTrue();
        status.AchievedAt.ShouldBe(journey.QuitDate.AddDays(1).AddMinutes(2 * 24 * 60));
    }
}

public class EncouragementServiceTests
{
    private readonly EncouragementService _service = new();

    private static ProgressStatistics StatsAfter(int smokeFreeDays)
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(smokeFreeDays).Smoking(20, 20, 10m);
        return builder.Build().GetStatistics(builder.Clock);
    }

    [Fact]
    public void Day_one_is_encouraged_as_the_start_of_the_journey()
    {
        _service.GenerateEncouragementMessage(StatsAfter(0)).ShouldStartWith("Today is Day 1!");
    }

    [Fact]
    public void The_first_full_day_gets_its_own_message()
    {
        _service.GenerateEncouragementMessage(StatsAfter(1))
            .ShouldStartWith("Congratulations on completing your first day!");
    }

    [Theory]
    [InlineData(5, "Amazing! 5 days smoke-free!")]
    [InlineData(10, "Week one complete!")]
    [InlineData(25, "Incredible progress! 25 days strong!")]
    [InlineData(60, "Over a month smoke-free!")]
    [InlineData(400, "Legendary status! 400 days smoke-free!")]
    public void The_message_moves_on_as_the_journey_lengthens(int smokeFreeDays, string expectedFragment)
    {
        var message = _service.GenerateEncouragementMessage(StatsAfter(smokeFreeDays));

        message.ShouldContain(expectedFragment);
        message.ShouldContain("$"); // every later message quotes the money saved
    }

    [Fact]
    public void The_message_counts_smoke_free_days_rather_than_days_elapsed()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(20).Smoking(20, 20, 10m)
            .SmokedDaysAgo(3)
            .SmokedDaysAgo(8)
            .SmokedDaysAgo(15);

        var message = _service.GenerateEncouragementMessage(builder.Build().GetStatistics(builder.Clock));

        message.ShouldContain("17 days strong");
        message.ShouldNotContain("20 days");
    }

    [Theory]
    [InlineData(7)]
    [InlineData(14)]
    [InlineData(21)]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(365)]
    public void Landmark_days_get_a_special_shout(int days)
    {
        var message = _service.GenerateSpecialMilestoneMessage(days);

        message.ShouldNotBeNull();
        message.ShouldContain("MILESTONE");
    }

    [Theory]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(100)]
    public void Ordinary_days_get_no_special_shout(int days)
    {
        _service.GenerateSpecialMilestoneMessage(days).ShouldBeNull();
    }

    [Theory]
    [InlineData(0, "3-5 minutes")]
    [InlineData(4, "hardest physical withdrawal")]
    [InlineData(10, "rewiring")]
    [InlineData(20, "solid foundation")]
    [InlineData(60, "champion")]
    public void Craving_advice_changes_as_the_journey_goes_on(int daysSmokeFree, string expectedFragment)
    {
        _service.GenerateCravingEncouragement(daysSmokeFree).ShouldContain(expectedFragment);
    }
}
