using DDD.BuildingBlocks;
using DietApi.Domain.Aggregates;
using DietApi.Features.BeerDays.GetBeerDayRange;
using DietApi.Features.BeerDays.MarkBeerDay;
using DietApi.Features.BeerDays.UnmarkBeerDay;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Marking, unmarking and reading beer days. Marking is bounded by the plan's start date and
/// idempotent; the calendar read returns only the dates that are beer days.
/// </summary>
public class BeerDayHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Marking_a_past_date_stores_one_beer_day()
    {
        var (plan, planRepo, userId) = APlan();
        var beer = FakeBeerDayRepository.Empty();
        var handler = new MarkBeerDayHandler(planRepo, beer, SignedInUser.WithId(userId));

        var result = await handler.Handle(new MarkBeerDayCommand(Today.AddDays(-2)), CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsBeerDay.ShouldBeTrue();
        beer.SaveCount.ShouldBe(1);
        beer.Stored.ShouldHaveSingleItem().Date.ShouldBe(Today.AddDays(-2));
    }

    [Fact]
    public async Task Marking_a_day_that_is_already_a_beer_day_changes_nothing()
    {
        var (plan, planRepo, userId) = APlan();
        var existing = BeerDayBuilder.ADay().ForUser(userId).ForPlan(plan.Id).DaysAgo(2).Build();
        var beer = FakeBeerDayRepository.Containing(existing);
        var handler = new MarkBeerDayHandler(planRepo, beer, SignedInUser.WithId(userId));

        var result = await handler.Handle(new MarkBeerDayCommand(Today.AddDays(-2)), CancellationToken.None);

        result!.IsBeerDay.ShouldBeTrue();
        beer.SaveCount.ShouldBe(0);
        beer.Stored.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Marking_a_future_date_is_refused()
    {
        var (_, planRepo, userId) = APlan();
        var handler = new MarkBeerDayHandler(planRepo, FakeBeerDayRepository.Empty(), SignedInUser.WithId(userId));

        await Should.ThrowAsync<DomainException>(
            handler.Handle(new MarkBeerDayCommand(Today.AddDays(1)), CancellationToken.None));
    }

    [Fact]
    public async Task Marking_without_a_plan_returns_null_so_the_endpoint_can_answer_404()
    {
        var handler = new MarkBeerDayHandler(
            FakeDietPlanRepository.Empty(), FakeBeerDayRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        var result = await handler.Handle(new MarkBeerDayCommand(Today.AddDays(-1)), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Marking_without_a_signed_in_member_is_refused()
    {
        var handler = new MarkBeerDayHandler(
            FakeDietPlanRepository.Empty(), FakeBeerDayRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new MarkBeerDayCommand(Today.AddDays(-1)), CancellationToken.None));
    }

    [Fact]
    public async Task Unmarking_removes_the_beer_day()
    {
        var (plan, planRepo, userId) = APlan();
        var existing = BeerDayBuilder.ADay().ForUser(userId).ForPlan(plan.Id).DaysAgo(2).Build();
        var beer = FakeBeerDayRepository.Containing(existing);
        var handler = new UnmarkBeerDayHandler(planRepo, beer, SignedInUser.WithId(userId));

        var result = await handler.Handle(new UnmarkBeerDayCommand(Today.AddDays(-2)), CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsBeerDay.ShouldBeFalse();
        beer.DeleteCount.ShouldBe(1);
        beer.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unmarking_a_date_that_is_not_a_beer_day_succeeds_and_does_nothing()
    {
        var (_, planRepo, userId) = APlan();
        var beer = FakeBeerDayRepository.Empty();
        var handler = new UnmarkBeerDayHandler(planRepo, beer, SignedInUser.WithId(userId));

        var result = await handler.Handle(new UnmarkBeerDayCommand(Today.AddDays(-2)), CancellationToken.None);

        result!.IsBeerDay.ShouldBeFalse();
        beer.DeleteCount.ShouldBe(0);
    }

    [Fact]
    public async Task The_range_returns_only_beer_days_within_the_plan()
    {
        var (plan, planRepo, userId) = APlan(startedDaysAgo: 5);

        var withinPlan = BeerDayBuilder.ADay().ForUser(userId).ForPlan(plan.Id).DaysAgo(2).PlanStartedDaysAgo(5).Build();
        var beforePlan = new BeerDayFromRawDate(userId, plan.Id, Today.AddDays(-20));

        var beer = FakeBeerDayRepository.Containing(withinPlan, beforePlan.Value);
        var handler = new GetBeerDayRangeHandler(planRepo, beer, SignedInUser.WithId(userId));

        var range = await handler.Handle(
            new GetBeerDayRangeQuery(Today.AddDays(-30), Today), CancellationToken.None);

        range.ShouldNotBeNull();
        range.Days.ShouldHaveSingleItem().ShouldBe(Today.AddDays(-2));
    }

    [Fact]
    public async Task Another_members_beer_days_are_not_returned()
    {
        var (plan, planRepo, userId) = APlan();
        var mine = BeerDayBuilder.ADay().ForUser(userId).ForPlan(plan.Id).DaysAgo(1).Build();
        var theirs = BeerDayBuilder.ADay().ForUser(Guid.NewGuid()).DaysAgo(1).Build();

        var handler = new GetBeerDayRangeHandler(
            planRepo, FakeBeerDayRepository.Containing(mine, theirs), SignedInUser.WithId(userId));

        var range = await handler.Handle(
            new GetBeerDayRangeQuery(Today.AddDays(-7), Today), CancellationToken.None);

        range!.Days.ShouldHaveSingleItem().ShouldBe(Today.AddDays(-1));
    }

    /// <summary>A beer day on a date that predates the plan - it could only come from an older plan.</summary>
    private sealed class BeerDayFromRawDate
    {
        public BeerDay Value { get; }

        public BeerDayFromRawDate(Guid userId, Guid planId, DateOnly date)
        {
            // Mark validates against a plan start far enough back to allow the old date.
            Value = BeerDay.Mark(planId, userId, date, date.AddDays(-1), date.ToDateTime(TimeOnly.MinValue));
        }
    }

    private static (DietPlan Plan, FakeDietPlanRepository Repo, Guid UserId) APlan(int startedDaysAgo = 30)
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(startedDaysAgo);
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }
}
