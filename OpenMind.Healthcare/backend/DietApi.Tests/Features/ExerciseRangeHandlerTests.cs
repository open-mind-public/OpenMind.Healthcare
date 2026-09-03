using DietApi.Domain.Aggregates;
using DietApi.Features.Exercise.GetExerciseRange;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// The calendar's read. Only days with activity come back, because absence is what the calendar
/// reads as no exercise - a row saying "no exercise" would be a fourth state by another name.
/// </summary>
public class ExerciseRangeHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Only_days_with_activity_are_returned()
    {
        var (plan, planRepo, userId) = APlan();

        var days = FakeExerciseDayRepository.Containing(
            ADay(plan, userId, daysAgo: 1, minutes: 45),
            ADay(plan, userId, daysAgo: 4, minutes: 30));

        var handler = new GetExerciseRangeHandler(planRepo, days, SignedInUser.WithId(userId));

        var range = await handler.Handle(
            new GetExerciseRangeQuery(Today.AddDays(-7), Today), CancellationToken.None);

        range.ShouldNotBeNull();
        range.Days.Count.ShouldBe(2);
        range.Days.Select(d => d.Date).ShouldBe([Today.AddDays(-4), Today.AddDays(-1)]);
        range.Days.ShouldAllBe(d => d.EntryCount > 0);
    }

    [Fact]
    public async Task A_range_with_no_activity_returns_an_empty_list_rather_than_an_error()
    {
        var (_, planRepo, userId) = APlan();
        var handler = new GetExerciseRangeHandler(
            planRepo, FakeExerciseDayRepository.Empty(), SignedInUser.WithId(userId));

        var range = await handler.Handle(
            new GetExerciseRangeQuery(Today.AddDays(-30), Today), CancellationToken.None);

        range.ShouldNotBeNull();
        range.Days.ShouldBeEmpty();
        range.From.ShouldBe(Today.AddDays(-30));
        range.To.ShouldBe(Today);
    }

    [Fact]
    public async Task Days_before_the_plan_started_are_excluded()
    {
        var (plan, planRepo, userId) = APlan(startedDaysAgo: 5);

        // A day that predates the plan - it could only exist from an older plan, and it is
        // neither activity within this plan nor the absence of it.
        var stray = ExerciseDayBuilder.ADay()
            .ForUser(userId).ForPlan(plan.Id)
            .DaysAgo(20).PlanStartedDaysAgo(30)
            .Did(FakeActivityTypeRepository.Running(), 45)
            .Build();

        var days = FakeExerciseDayRepository.Containing(stray, ADay(plan, userId, daysAgo: 2, minutes: 30));

        var handler = new GetExerciseRangeHandler(planRepo, days, SignedInUser.WithId(userId));

        var range = await handler.Handle(
            new GetExerciseRangeQuery(Today.AddDays(-30), Today), CancellationToken.None);

        range.ShouldNotBeNull();
        range.Days.Count.ShouldBe(1);
        range.Days.Single().Date.ShouldBe(Today.AddDays(-2));
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = new GetExerciseRangeHandler(
            FakeDietPlanRepository.Empty(), FakeExerciseDayRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        var range = await handler.Handle(
            new GetExerciseRangeQuery(Today.AddDays(-7), Today), CancellationToken.None);

        range.ShouldBeNull();
    }

    [Fact]
    public async Task Another_members_days_are_not_returned()
    {
        var (plan, planRepo, userId) = APlan();
        var mine = ADay(plan, userId, daysAgo: 1, minutes: 45);
        var theirs = ExerciseDayBuilder.ADay()
            .ForUser(Guid.NewGuid()).DaysAgo(1)
            .Did(FakeActivityTypeRepository.Running(), 90)
            .Build();

        var handler = new GetExerciseRangeHandler(
            planRepo, FakeExerciseDayRepository.Containing(mine, theirs), SignedInUser.WithId(userId));

        var range = await handler.Handle(
            new GetExerciseRangeQuery(Today.AddDays(-7), Today), CancellationToken.None);

        range.ShouldNotBeNull();
        range.Days.Count.ShouldBe(1);
        range.Days.Single().TotalMinutes.ShouldBe(45);
    }

    [Fact]
    public async Task Fetching_a_range_without_a_signed_in_member_is_refused()
    {
        var handler = new GetExerciseRangeHandler(
            FakeDietPlanRepository.Empty(), FakeExerciseDayRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetExerciseRangeQuery(Today.AddDays(-7), Today), CancellationToken.None));
    }

    private static ExerciseDay ADay(DietPlan plan, Guid userId, int daysAgo, int minutes) =>
        ExerciseDayBuilder.ADay()
            .ForUser(userId).ForPlan(plan.Id).DaysAgo(daysAgo)
            .Did(FakeActivityTypeRepository.Running(), minutes)
            .Build();

    private static (DietPlan Plan, FakeDietPlanRepository Repo, Guid UserId) APlan(int startedDaysAgo = 30)
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(startedDaysAgo).Weighing(70m);
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }
}
