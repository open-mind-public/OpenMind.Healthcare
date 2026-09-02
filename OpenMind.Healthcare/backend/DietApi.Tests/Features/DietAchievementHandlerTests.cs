using PlanAggregate = DietApi.Domain.Aggregates.DietPlan;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietAchievements.CheckNewDietAchievements;
using DietApi.Features.DietAchievements.GetDietAchievements;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for the achievement use cases.
/// </summary>
public class DietAchievementHandlerTests
{
    private readonly StreakCalculator _streaks = new();
    private readonly DietAchievementStatusService _statuses = new();

    [Fact]
    public async Task Achievements_come_back_with_the_member_s_state_against_each()
    {
        var (plan, planRepo, dayRepo, builder) = APlanWithDays(loggedDays: 1);
        var achievements = FakeDietAchievementRepository.Containing(
            FakeDietAchievementRepository.FirstDayLogged(),
            FakeDietAchievementRepository.WeekOnTarget());

        var handler = new GetDietAchievementsHandler(
            planRepo, dayRepo, achievements, _streaks, _statuses, SignedInUser.WithId(builder.UserId));

        var result = await handler.Handle(new GetDietAchievementsQuery(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Achievements.Count.ShouldBe(2);
        result.Achievements.Single(a => a.Name == "First day logged").Unlocked.ShouldBeTrue();

        var week = result.Achievements.Single(a => a.Name == "A week on target");
        week.Unlocked.ShouldBeFalse();
        week.Remaining.ShouldBe(6);
        _ = plan;
    }

    [Fact]
    public async Task Only_unlocked_achievements_come_back_when_asked_for_those()
    {
        var (_, planRepo, dayRepo, builder) = APlanWithDays(loggedDays: 1);
        var achievements = FakeDietAchievementRepository.Containing(
            FakeDietAchievementRepository.FirstDayLogged(),
            FakeDietAchievementRepository.WeekOnTarget());

        var handler = new GetDietAchievementsHandler(
            planRepo, dayRepo, achievements, _streaks, _statuses, SignedInUser.WithId(builder.UserId));

        var result = await handler.Handle(new GetDietAchievementsQuery(UnlockedOnly: true), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Achievements.Count.ShouldBe(1);
        result.Achievements.Single().Name.ShouldBe("First day logged");
    }

    [Fact]
    public async Task Checking_returns_the_newly_unlocked_and_persists_them()
    {
        var (_, planRepo, dayRepo, builder) = APlanWithDays(loggedDays: 1);
        var achievements = FakeDietAchievementRepository.Containing(FakeDietAchievementRepository.FirstDayLogged());

        var handler = new CheckNewDietAchievementsHandler(
            planRepo, dayRepo, achievements, _streaks, _statuses, SignedInUser.WithId(builder.UserId));

        var result = await handler.Handle(new CheckNewDietAchievementsCommand(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.NewlyUnlocked.Count.ShouldBe(1);
        planRepo.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Checking_twice_awards_nothing_the_second_time()
    {
        var (_, planRepo, dayRepo, builder) = APlanWithDays(loggedDays: 1);
        var achievements = FakeDietAchievementRepository.Containing(FakeDietAchievementRepository.FirstDayLogged());

        var handler = new CheckNewDietAchievementsHandler(
            planRepo, dayRepo, achievements, _streaks, _statuses, SignedInUser.WithId(builder.UserId));

        await handler.Handle(new CheckNewDietAchievementsCommand(), CancellationToken.None);
        var second = await handler.Handle(new CheckNewDietAchievementsCommand(), CancellationToken.None);

        second.ShouldNotBeNull();
        second.NewlyUnlocked.ShouldBeEmpty();
        planRepo.SaveCount.ShouldBe(1);   // nothing new, so nothing saved
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = new GetDietAchievementsHandler(
            FakeDietPlanRepository.Empty(), FakeLoggedDayRepository.Empty(),
            FakeDietAchievementRepository.Containing(), _streaks, _statuses, SignedInUser.WithId(Guid.NewGuid()));

        (await handler.Handle(new GetDietAchievementsQuery(), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Reading_achievements_without_a_signed_in_member_is_refused()
    {
        var handler = new GetDietAchievementsHandler(
            FakeDietPlanRepository.Empty(), FakeLoggedDayRepository.Empty(),
            FakeDietAchievementRepository.Containing(), _streaks, _statuses, SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetDietAchievementsQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Checking_without_a_signed_in_member_is_refused()
    {
        var handler = new CheckNewDietAchievementsHandler(
            FakeDietPlanRepository.Empty(), FakeLoggedDayRepository.Empty(),
            FakeDietAchievementRepository.Containing(), _streaks, _statuses, SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new CheckNewDietAchievementsCommand(), CancellationToken.None));
    }

    private static (PlanAggregate, FakeDietPlanRepository, FakeLoggedDayRepository, DietPlanBuilder)
        APlanWithDays(int loggedDays)
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30);
        var plan = builder.Build();

        var days = Enumerable.Range(0, loggedDays)
            .Select(i => LoggedDayBuilder.ADay()
                .ForUser(builder.UserId).ForPlan(plan.Id)
                .PlanStartedDaysAgo(30).DaysAgo(i)
                .Targeting(2100)
                .Ate(FakeFoodLibraryRepository.Oats())
                .Build())
            .ToArray();

        return (plan, FakeDietPlanRepository.Containing(plan), FakeLoggedDayRepository.Containing(days), builder);
    }
}
