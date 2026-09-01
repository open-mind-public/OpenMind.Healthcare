using DDD.BuildingBlocks;
using QuitSmokingApi.Domain.Aggregates;
using QuitSmokingApi.Domain.ValueObjects;

namespace QuitSmokingApi.Tests.Domain;

public class MilestoneTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(7, 7)]
    [InlineData(29, 21)]
    [InlineData(30, 30)]
    [InlineData(400, 365)]
    public void The_current_milestone_is_the_highest_one_already_reached(int daysSmokeFree, int expectedRequiredDays)
    {
        Milestone.GetMilestoneForDays(daysSmokeFree).RequiredDays.ShouldBe(expectedRequiredDays);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 3)]
    [InlineData(30, 42)]
    [InlineData(180, 365)]
    public void The_next_milestone_is_the_nearest_one_still_ahead(int daysSmokeFree, int expectedRequiredDays)
    {
        Milestone.GetNextMilestone(daysSmokeFree)!.RequiredDays.ShouldBe(expectedRequiredDays);
    }

    [Theory]
    [InlineData(365)]
    [InlineData(500)]
    public void There_is_nothing_left_to_reach_after_a_year(int daysSmokeFree)
    {
        Milestone.GetNextMilestone(daysSmokeFree).ShouldBeNull();
    }

    [Fact]
    public void A_milestone_reports_how_many_days_are_left_and_never_a_negative_number()
    {
        Milestone.OneMonth.GetDaysRemaining(20).ShouldBe(10);
        Milestone.OneMonth.GetDaysRemaining(30).ShouldBe(0);
        Milestone.OneMonth.GetDaysRemaining(45).ShouldBe(0);
    }

    [Fact]
    public void A_milestone_is_reached_on_the_day_it_requires()
    {
        Milestone.OneWeek.IsAchieved(6).ShouldBeFalse();
        Milestone.OneWeek.IsAchieved(7).ShouldBeTrue();
        Milestone.OneWeek.IsAchieved(8).ShouldBeTrue();
    }

    [Fact]
    public void Milestones_run_from_the_first_step_to_a_full_year()
    {
        var all = Milestone.GetAll();

        all.First().RequiredDays.ShouldBe(0);
        all.Last().RequiredDays.ShouldBe(365);
        all.Select(m => m.RequiredDays).ShouldBeUnique();
    }
}

public class AchievementTests
{
    [Fact]
    public void An_achievement_needs_a_name()
    {
        var create = () => Achievement.Create("  ", "desc", "🏅", 7, AchievementCategory.Milestone);

        create.ShouldThrow<DomainException>().Message.ShouldBe("Achievement name is required");
    }

    [Fact]
    public void An_achievement_cannot_require_a_negative_number_of_days()
    {
        var create = () => Achievement.Create("One week", "desc", "🏅", -1, AchievementCategory.Milestone);

        create.ShouldThrow<DomainException>().Message.ShouldBe("Required days cannot be negative");
    }

    [Fact]
    public void An_achievement_unlocks_once_enough_smoke_free_days_are_banked()
    {
        var achievement = Achievement.Create("One week", "desc", "🏅", 7, AchievementCategory.Milestone);

        achievement.IsUnlockedFor(6).ShouldBeFalse();
        achievement.IsUnlockedFor(7).ShouldBeTrue();
        achievement.IsUnlockedFor(30).ShouldBeTrue();
    }

    [Fact]
    public void An_achievement_is_only_freshly_unlocked_on_the_exact_day_it_is_earned()
    {
        var achievement = Achievement.Create("One week", "desc", "🏅", 7, AchievementCategory.Milestone);

        achievement.IsExactlyUnlockedFor(7).ShouldBeTrue();
        achievement.IsExactlyUnlockedFor(8).ShouldBeFalse();
    }

    [Fact]
    public void Progress_towards_an_achievement_is_a_share_of_the_days_it_needs()
    {
        var achievement = Achievement.Create("One month", "desc", "👑", 30, AchievementCategory.Milestone);

        achievement.GetProgress(0).ShouldBe(0);
        achievement.GetProgress(15).ShouldBe(50);
        achievement.GetProgress(30).ShouldBe(100);
        achievement.GetProgress(60).ShouldBe(100); // never overshoots
    }

    [Fact]
    public void An_achievement_that_needs_no_days_is_complete_from_the_start()
    {
        var achievement = Achievement.Create("First step", "desc", "🌟", 0, AchievementCategory.Milestone);

        achievement.GetProgress(0).ShouldBe(100);
    }
}
