using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietAnalytics.GetHabitInsights;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// The analytics "Habits" read: it gathers beer dates, exercise dates and logged days, and hands
/// them to <see cref="HabitAnalyser"/>. These tests cover the gathering and the failure paths - the
/// arithmetic is proven in <c>HabitAnalyserTests</c>.
/// </summary>
public class HabitInsightsHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task It_reports_beer_and_exercise_frequency_for_the_period()
    {
        var planBuilder = DietPlanBuilder.APlan().StartedDaysAgo(60);
        var plan = planBuilder.Build();
        var userId = planBuilder.UserId;

        var analytics = FakeDietAnalyticsRepository.For(userId)
            .WithSimpleDay(Today.AddDays(-1), calories: 2600, targetCalories: 2100)  // over, beer
            .WithSimpleDay(Today.AddDays(-2), calories: 1800, targetCalories: 2100); // on target

        var beer = FakeBeerDayRepository.Containing(
            BeerDayBuilder.ADay().ForUser(userId).ForPlan(plan.Id).DaysAgo(1).PlanStartedDaysAgo(60).Build());

        var exercise = FakeExerciseDayRepository.Containing(
            ExerciseDayBuilder.ADay().ForUser(userId).ForPlan(plan.Id).DaysAgo(2).PlanStartedDaysAgo(60)
                .Did(FakeActivityTypeRepository.Running(), 30).Build());

        var handler = new GetHabitInsightsHandler(
            FakeDietPlanRepository.Containing(plan),
            analytics,
            beer,
            exercise,
            new AnalysisPeriodResolver(),
            new HabitAnalyser(),
            SignedInUser.WithId(userId));

        var result = await handler.Handle(new GetHabitInsightsQuery(PeriodPreset.Month), CancellationToken.None);

        result.ShouldNotBeNull();
        result.BeerDays.ShouldBe(1);
        result.ExerciseDays.ShouldBe(1);
        result.OnBeerDays.Days.ShouldBe(1);
        result.OnBeerDays.OverTargetDays.ShouldBe(1);
        result.OnNonBeerDays.Days.ShouldBe(result.InPlanDays - 1);
        result.Period.Preset.ShouldBe(PeriodPreset.Month);
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = new GetHabitInsightsHandler(
            FakeDietPlanRepository.Empty(),
            FakeDietAnalyticsRepository.For(Guid.NewGuid()),
            FakeBeerDayRepository.Empty(),
            FakeExerciseDayRepository.Empty(),
            new AnalysisPeriodResolver(),
            new HabitAnalyser(),
            SignedInUser.WithId(Guid.NewGuid()));

        var result = await handler.Handle(new GetHabitInsightsQuery(PeriodPreset.Month), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Without_a_signed_in_member_it_is_refused()
    {
        var handler = new GetHabitInsightsHandler(
            FakeDietPlanRepository.Empty(),
            FakeDietAnalyticsRepository.For(Guid.NewGuid()),
            FakeBeerDayRepository.Empty(),
            FakeExerciseDayRepository.Empty(),
            new AnalysisPeriodResolver(),
            new HabitAnalyser(),
            SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetHabitInsightsQuery(PeriodPreset.Month), CancellationToken.None));
    }
}
