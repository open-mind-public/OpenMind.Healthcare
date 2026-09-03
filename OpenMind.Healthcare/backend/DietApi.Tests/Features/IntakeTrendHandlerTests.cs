using DietPlanAggregate = DietApi.Domain.Aggregates.DietPlan;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietAnalytics.GetIntakeTrend;
using DietApi.Tests.TestSupport;
using static DietApi.Tests.TestSupport.FakeDietAnalyticsRepository;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for the daily trend.
/// </summary>
public class IntakeTrendHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task A_member_gets_one_point_per_calendar_day_with_gaps_flagged()
    {
        var (_, planRepo, userId) = APlan();

        var analytics = FakeDietAnalyticsRepository.For(userId)
            .WithSimpleDay(Today, 2000)
            .WithSimpleDay(Today.AddDays(-2), 1800);

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetIntakeTrendQuery(PeriodPreset.Week), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Points.Count.ShouldBe(7);
        response.LoggedDays.ShouldBe(2);
        response.Points.Count(p => p.Logged).ShouldBe(2);
        response.Points.Count(p => !p.Logged).ShouldBe(5);
    }

    [Fact]
    public async Task The_points_carry_the_target_across_the_gaps()
    {
        var (_, planRepo, userId) = APlan();

        var analytics = FakeDietAnalyticsRepository.For(userId)
            .WithSimpleDay(Today.AddDays(-6), 2000, targetCalories: 2100);

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetIntakeTrendQuery(PeriodPreset.Week), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Points.ShouldAllBe(p => p.TargetKilocalories == 2100);
        response.PeakKilocalories.ShouldBe(2100);
    }

    [Fact]
    public async Task A_member_with_nothing_logged_gets_a_full_row_of_gaps_rather_than_an_error()
    {
        var (_, planRepo, userId) = APlan();

        var response = await Handler(planRepo, FakeDietAnalyticsRepository.For(userId), userId)
            .Handle(new GetIntakeTrendQuery(PeriodPreset.Month), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Points.Count.ShouldBe(response.Period.TotalDays);
        response.LoggedDays.ShouldBe(0);
        response.Points.ShouldAllBe(p => !p.Logged);
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = Handler(
            FakeDietPlanRepository.Empty(), FakeDietAnalyticsRepository.For(Guid.NewGuid()), Guid.NewGuid());

        (await handler.Handle(new GetIntakeTrendQuery(PeriodPreset.Month), CancellationToken.None))
            .ShouldBeNull();
    }

    [Fact]
    public async Task Another_members_days_do_not_appear()
    {
        var (_, planRepo, userId) = APlan();

        var someoneElses = FakeDietAnalyticsRepository.For(Guid.NewGuid()).WithSimpleDay(Today, 3000);

        var response = await Handler(planRepo, someoneElses, userId)
            .Handle(new GetIntakeTrendQuery(PeriodPreset.Week), CancellationToken.None);

        response.ShouldNotBeNull();
        response.LoggedDays.ShouldBe(0);
    }

    [Fact]
    public async Task Asking_without_a_signed_in_member_is_refused()
    {
        var handler = new GetIntakeTrendHandler(
            FakeDietPlanRepository.Empty(),
            FakeDietAnalyticsRepository.For(Guid.NewGuid()),
            new AnalysisPeriodResolver(),
            new TrendAnalyser(),
            SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetIntakeTrendQuery(PeriodPreset.Month), CancellationToken.None));
    }

    private static GetIntakeTrendHandler Handler(
        FakeDietPlanRepository planRepo, FakeDietAnalyticsRepository analytics, Guid userId) =>
        new(planRepo, analytics, new AnalysisPeriodResolver(), new TrendAnalyser(),
            SignedInUser.WithId(userId));

    private static (DietPlanAggregate Plan, FakeDietPlanRepository Repo, Guid UserId) APlan(int startedDaysAgo = 60)
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(startedDaysAgo).WithTargets(2100);
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }
}
