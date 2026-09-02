using LoggedDayAggregate = DietApi.Domain.Aggregates.LoggedDay;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietStats.GetDietStats;
using DietApi.Features.FoodLog.GetDayRange;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for the history and statistics use cases.
/// </summary>
public class DietStatsHandlerTests
{
    private readonly StreakCalculator _calculator = new();

    [Fact]
    public async Task Statistics_reflect_the_member_s_logged_days()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30);
        var plan = builder.Build();
        var planRepo = FakeDietPlanRepository.Containing(plan);

        var dayRepo = FakeLoggedDayRepository.Containing(
            Day(builder.UserId, plan.Id, 2),
            Day(builder.UserId, plan.Id, 1),
            Day(builder.UserId, plan.Id, 0));

        var handler = new GetDietStatsHandler(planRepo, dayRepo, _calculator, SignedInUser.WithId(builder.UserId));

        var stats = await handler.Handle(new GetDietStatsQuery(), CancellationToken.None);

        stats.ShouldNotBeNull();
        stats.TotalDaysLogged.ShouldBe(3);
        stats.CurrentStreakDays.ShouldBe(3);
        stats.PlanStartDate.ShouldBe(plan.StartDate);
    }

    [Fact]
    public async Task A_member_with_a_plan_and_no_entries_gets_zeros_not_an_error()
    {
        var builder = DietPlanBuilder.APlan();
        var plan = builder.Build();
        var handler = new GetDietStatsHandler(
            FakeDietPlanRepository.Containing(plan), FakeLoggedDayRepository.Empty(),
            _calculator, SignedInUser.WithId(builder.UserId));

        var stats = await handler.Handle(new GetDietStatsQuery(), CancellationToken.None);

        stats.ShouldNotBeNull();
        stats.TotalDaysLogged.ShouldBe(0);
        stats.CurrentStreakDays.ShouldBe(0);
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = new GetDietStatsHandler(
            FakeDietPlanRepository.Empty(), FakeLoggedDayRepository.Empty(),
            _calculator, SignedInUser.WithId(Guid.NewGuid()));

        (await handler.Handle(new GetDietStatsQuery(), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Statistics_without_a_signed_in_member_are_refused()
    {
        var handler = new GetDietStatsHandler(
            FakeDietPlanRepository.Empty(), FakeLoggedDayRepository.Empty(),
            _calculator, SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetDietStatsQuery(), CancellationToken.None));
    }

    // --- Day range --------------------------------------------------------

    [Fact]
    public async Task A_range_marks_days_before_the_plan_as_outside_it_rather_than_missed()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(3);
        var plan = builder.Build();
        var handler = new GetDayRangeHandler(
            FakeDietPlanRepository.Containing(plan), FakeLoggedDayRepository.Empty(),
            SignedInUser.WithId(builder.UserId));

        var range = await handler.Handle(
            new GetDayRangeQuery(builder.DaysAgo(6), builder.Today), CancellationToken.None);

        range.ShouldNotBeNull();
        range.Days.Count.ShouldBe(7);

        var beforePlan = range.Days.Where(d => d.Date < plan.StartDate).ToList();
        beforePlan.Count.ShouldBe(3);
        beforePlan.ShouldAllBe(d => !d.WithinPlan);
        beforePlan.ShouldAllBe(d => d.State == null);

        var withinPlan = range.Days.Where(d => d.Date >= plan.StartDate).ToList();
        withinPlan.ShouldAllBe(d => d.WithinPlan);
    }

    [Fact]
    public async Task Unlogged_days_inside_the_plan_are_marked_not_logged()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(5);
        var plan = builder.Build();
        var dayRepo = FakeLoggedDayRepository.Containing(Day(builder.UserId, plan.Id, 1));
        var handler = new GetDayRangeHandler(
            FakeDietPlanRepository.Containing(plan), dayRepo, SignedInUser.WithId(builder.UserId));

        var range = await handler.Handle(
            new GetDayRangeQuery(builder.DaysAgo(3), builder.Today), CancellationToken.None);

        range.ShouldNotBeNull();
        range.Days.Single(d => d.Date == builder.DaysAgo(1)).State.ShouldBe(DayState.OnTarget);
        range.Days.Single(d => d.Date == builder.Today).State.ShouldBe(DayState.NotLogged);
    }

    [Fact]
    public async Task A_range_for_a_member_with_no_plan_returns_null()
    {
        var handler = new GetDayRangeHandler(
            FakeDietPlanRepository.Empty(), FakeLoggedDayRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        (await handler.Handle(new GetDayRangeQuery(today.AddDays(-7), today), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task A_range_without_a_signed_in_member_is_refused()
    {
        var handler = new GetDayRangeHandler(
            FakeDietPlanRepository.Empty(), FakeLoggedDayRepository.Empty(), SignedInUser.Anonymous());

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetDayRangeQuery(today.AddDays(-7), today), CancellationToken.None));
    }

    private static LoggedDayAggregate Day(Guid userId, Guid planId, int daysAgo) =>
        LoggedDayBuilder.ADay()
            .ForUser(userId)
            .ForPlan(planId)
            .PlanStartedDaysAgo(30)
            .DaysAgo(daysAgo)
            .Targeting(2100)
            .Ate(FakeFoodLibraryRepository.Oats())
            .Build();
}
