using DietApi.Domain.Aggregates;
using DietApi.Domain.Services;
using DietApi.Features.Exercise.GetActivitySummary;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for the weekly summary.
/// </summary>
public class ActivitySummaryHandlerTests
{
    [Fact]
    public async Task A_member_with_activity_sees_their_week_and_the_one_before()
    {
        var (plan, planRepo, userId) = APlan();

        var days = FakeExerciseDayRepository.Containing(
            ADay(plan, userId, daysAgo: 1, minutes: 45),
            ADay(plan, userId, daysAgo: 3, minutes: 30),
            ADay(plan, userId, daysAgo: 9, minutes: 60));

        var handler = new GetActivitySummaryHandler(
            planRepo, days, new ActivitySummaryCalculator(), SignedInUser.WithId(userId));

        var summary = await handler.Handle(new GetActivitySummaryQuery(), CancellationToken.None);

        summary.ShouldNotBeNull();
        summary.WindowDays.ShouldBe(7);
        summary.ActiveDays.ShouldBe(2);
        summary.TotalMinutes.ShouldBe(75);
        summary.PreviousWindowActiveDays.ShouldBe(1);
        summary.PreviousWindowMinutes.ShouldBe(60);
    }

    [Fact]
    public async Task A_member_with_a_plan_and_no_activity_gets_zeros_rather_than_an_error()
    {
        var (_, planRepo, userId) = APlan();

        var handler = new GetActivitySummaryHandler(
            planRepo, FakeExerciseDayRepository.Empty(), new ActivitySummaryCalculator(), SignedInUser.WithId(userId));

        var summary = await handler.Handle(new GetActivitySummaryQuery(), CancellationToken.None);

        summary.ShouldNotBeNull();
        summary.ActiveDays.ShouldBe(0);
        summary.TotalMinutes.ShouldBe(0);
        summary.TotalKilocalories.ShouldBe(0);
        summary.PreviousWindowActiveDays.ShouldBe(0);
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = new GetActivitySummaryHandler(
            FakeDietPlanRepository.Empty(),
            FakeExerciseDayRepository.Empty(),
            new ActivitySummaryCalculator(),
            SignedInUser.WithId(Guid.NewGuid()));

        (await handler.Handle(new GetActivitySummaryQuery(), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Another_members_activity_is_not_counted()
    {
        var (plan, planRepo, userId) = APlan();

        var theirs = ExerciseDayBuilder.ADay()
            .ForUser(Guid.NewGuid()).DaysAgo(1)
            .Did(FakeActivityTypeRepository.Running(), 120)
            .Build();

        var days = FakeExerciseDayRepository.Containing(ADay(plan, userId, daysAgo: 1, minutes: 45), theirs);

        var handler = new GetActivitySummaryHandler(
            planRepo, days, new ActivitySummaryCalculator(), SignedInUser.WithId(userId));

        var summary = await handler.Handle(new GetActivitySummaryQuery(), CancellationToken.None);

        summary.ShouldNotBeNull();
        summary.ActiveDays.ShouldBe(1);
        summary.TotalMinutes.ShouldBe(45);
    }

    [Fact]
    public async Task Asking_for_the_summary_without_a_signed_in_member_is_refused()
    {
        var handler = new GetActivitySummaryHandler(
            FakeDietPlanRepository.Empty(),
            FakeExerciseDayRepository.Empty(),
            new ActivitySummaryCalculator(),
            SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetActivitySummaryQuery(), CancellationToken.None));
    }

    private static ExerciseDay ADay(DietPlan plan, Guid userId, int daysAgo, int minutes) =>
        ExerciseDayBuilder.ADay()
            .ForUser(userId).ForPlan(plan.Id).DaysAgo(daysAgo)
            .Did(FakeActivityTypeRepository.Running(), minutes)
            .Build();

    private static (DietPlan Plan, FakeDietPlanRepository Repo, Guid UserId) APlan()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(60).Weighing(70m);
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }
}
