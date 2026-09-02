using DietApi.Domain.Aggregates;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// Achievements are stored once earned, never derived. The tests that matter most here are the
/// ones proving a badge cannot be taken back.
/// </summary>
public class DietAchievementTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly DietAchievementStatusService _service = new();

    private static readonly DietAchievement WeekOnTarget =
        DietAchievement.Create("A week on target", "Seven days in a row.", "🥗",
            AchievementCriterion.ConsecutiveOnTargetDays, 7);

    private static readonly DietAchievement HundredDaysLogged =
        DietAchievement.Create("One hundred days logged", "A hundred days.", "💯",
            AchievementCriterion.TotalDaysLogged, 100);

    [Fact]
    public void Meeting_the_criteria_unlocks_the_achievement_with_today_s_date()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var stats = Stats(currentStreak: 7, longestStreak: 7, daysLogged: 7);

        var statuses = _service.Evaluate(plan, stats, [WeekOnTarget]);

        statuses.Single().Unlocked.ShouldBeTrue();
        statuses.Single().EarnedOn.ShouldBe(Today);
        plan.UnlockedAchievements.Count.ShouldBe(1);
    }

    [Fact]
    public void Evaluating_twice_awards_nothing_the_second_time()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var stats = Stats(currentStreak: 7, longestStreak: 7, daysLogged: 7);

        _service.Evaluate(plan, stats, [WeekOnTarget]);
        _service.Evaluate(plan, stats, [WeekOnTarget]);

        plan.UnlockedAchievements.Count.ShouldBe(1);
    }

    [Fact]
    public void An_earned_achievement_survives_the_statistics_falling_back_below_its_threshold()
    {
        // The member deletes a mis-logged entry and their streak collapses. The badge stays.
        var plan = DietPlanBuilder.APlan().Build();

        _service.Evaluate(plan, Stats(currentStreak: 7, longestStreak: 7, daysLogged: 7), [WeekOnTarget]);

        var afterDeletion = _service.Evaluate(
            plan, Stats(currentStreak: 0, longestStreak: 0, daysLogged: 0), [WeekOnTarget]);

        afterDeletion.Single().Unlocked.ShouldBeTrue();
        afterDeletion.Single().EarnedOn.ShouldBe(Today);
        plan.UnlockedAchievements.Count.ShouldBe(1);
    }

    [Fact]
    public void An_earned_achievement_keeps_its_original_date()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var earlier = Today.AddDays(-20);

        plan.Unlock(WeekOnTarget.Id, earlier);

        var statuses = _service.Evaluate(plan, Stats(currentStreak: 7, longestStreak: 7, daysLogged: 7), [WeekOnTarget]);

        statuses.Single().EarnedOn.ShouldBe(earlier);
    }

    [Fact]
    public void A_locked_achievement_reports_what_remains()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var stats = Stats(currentStreak: 2, longestStreak: 3, daysLogged: 12);

        var statuses = _service.Evaluate(plan, stats, [WeekOnTarget, HundredDaysLogged]);

        var week = statuses.Single(s => s.Achievement.Id == WeekOnTarget.Id);
        week.Unlocked.ShouldBeFalse();
        week.Remaining.ShouldBe(4);   // best streak of 3, needs 7

        var hundred = statuses.Single(s => s.Achievement.Id == HundredDaysLogged.Id);
        hundred.Remaining.ShouldBe(88);
    }

    [Fact]
    public void A_streak_achievement_counts_the_member_s_best_run_not_only_their_current_one()
    {
        // Losing a streak should not lose the badge you already qualified for.
        var plan = DietPlanBuilder.APlan().Build();
        var stats = Stats(currentStreak: 1, longestStreak: 8, daysLogged: 20);

        _service.Evaluate(plan, stats, [WeekOnTarget]).Single().Unlocked.ShouldBeTrue();
    }

    [Fact]
    public void Unlocking_the_same_achievement_twice_directly_is_also_a_no_op()
    {
        var plan = DietPlanBuilder.APlan().Build();

        plan.Unlock(WeekOnTarget.Id, Today);
        plan.Unlock(WeekOnTarget.Id, Today.AddDays(1));

        plan.UnlockedAchievements.Count.ShouldBe(1);
        plan.UnlockedAchievements.Single().EarnedOn.ShouldBe(Today);
    }

    [Fact]
    public void A_days_on_plan_achievement_measures_time_not_effort()
    {
        var plan = DietPlanBuilder.APlan().Build();
        var monthOnPlan = DietAchievement.Create("A month on plan", "Thirty days.", "📅",
            AchievementCriterion.DaysOnPlan, 30);

        var stats = DietStatistics.Create(0, 0, 0, 0, 0, Today.AddDays(-40), daysOnPlan: 41);

        _service.Evaluate(plan, stats, [monthOnPlan]).Single().Unlocked.ShouldBeTrue();
    }

    private static DietStatistics Stats(int currentStreak, int longestStreak, int daysLogged) =>
        DietStatistics.Create(currentStreak, longestStreak, daysLogged, 2000, daysLogged, Today.AddDays(-30), 31);
}
