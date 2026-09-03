using DDD.BuildingBlocks;
using DietPlanAggregate = DietApi.Domain.Aggregates.DietPlan;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietAnalytics.GetObservations;
using DietApi.Tests.Domain;
using DietApi.Tests.TestSupport;
using static DietApi.Tests.TestSupport.FakeDietAnalyticsRepository;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for the observations.
/// </summary>
public class ObservationsHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task A_member_with_a_clear_pattern_sees_it_named_with_its_figure()
    {
        var (_, planRepo, userId) = APlan();

        // A month of days with a third of the energy logged late in the evening.
        var analytics = FakeDietAnalyticsRepository.For(userId);
        for (var i = 0; i < 24; i++)
        {
            var date = Today.AddDays(-i);
            analytics.WithDay(date,
            [
                At(date, 12, 0, 1400),
                At(date, 22, 0, 700)
            ]);
        }

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetObservationsQuery(PeriodPreset.Month, 0), CancellationToken.None);

        response.ShouldNotBeNull();
        response.NothingStoodOut.ShouldBeFalse();
        response.Observations.ShouldNotBeEmpty();
        response.Observations.ShouldAllBe(o => !string.IsNullOrWhiteSpace(o.Figure));
        response.Observations.ShouldAllBe(o => o.BasedOnDays > 0);
    }

    [Fact]
    public async Task A_member_below_every_minimum_is_told_what_it_would_take()
    {
        var (_, planRepo, userId) = APlan();

        var analytics = FakeDietAnalyticsRepository.For(userId);
        for (var i = 0; i < 9; i++)
        {
            var date = Today.AddDays(-i);
            analytics.WithDay(date, [At(date, 22, 0, 2000)]);
        }

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetObservationsQuery(PeriodPreset.Month, 0), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Observations.ShouldBeEmpty();
        response.NothingStoodOut.ShouldBeTrue();
        response.MinimumDaysForAnyObservation.ShouldBe(14);
        response.Period.LoggedDays.ShouldBe(9);
    }

    [Fact]
    public async Task The_same_request_twice_returns_the_same_observations()
    {
        var (_, planRepo, userId) = APlan();

        var analytics = FakeDietAnalyticsRepository.For(userId);
        for (var i = 0; i < 24; i++)
        {
            var date = Today.AddDays(-i);
            analytics.WithDay(date, [At(date, 22, 0, 1800), At(date, 12, 0, 400)]);
        }

        var handler = Handler(planRepo, analytics, userId);

        var first = await handler.Handle(new GetObservationsQuery(PeriodPreset.Month, 0), CancellationToken.None);
        var second = await handler.Handle(new GetObservationsQuery(PeriodPreset.Month, 0), CancellationToken.None);

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        first.Observations.Select(o => o.Text).ShouldBe(second.Observations.Select(o => o.Text));
    }

    [Fact]
    public async Task No_two_observations_describe_the_same_family()
    {
        var (_, planRepo, userId) = APlan();

        var analytics = FakeDietAnalyticsRepository.For(userId);
        for (var i = 0; i < 28; i++)
        {
            var date = Today.AddDays(-i);
            var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            // Late eating and heavy weekends together - both are Timing.
            analytics.WithDay(date, [At(date, 22, 0, weekend ? 3000 : 1800)]);
        }

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetObservationsQuery(PeriodPreset.Month, 0), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Observations.Select(o => o.Family).Distinct().Count()
            .ShouldBe(response.Observations.Count);
    }

    [Fact]
    public async Task A_member_with_a_plan_and_nothing_logged_is_told_nothing_stood_out()
    {
        var (_, planRepo, userId) = APlan();

        var response = await Handler(planRepo, FakeDietAnalyticsRepository.For(userId), userId)
            .Handle(new GetObservationsQuery(PeriodPreset.Month, 0), CancellationToken.None);

        response.ShouldNotBeNull();
        response.NothingStoodOut.ShouldBeTrue();
        response.Observations.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = Handler(
            FakeDietPlanRepository.Empty(), FakeDietAnalyticsRepository.For(Guid.NewGuid()), Guid.NewGuid());

        (await handler.Handle(new GetObservationsQuery(PeriodPreset.Month, 0), CancellationToken.None))
            .ShouldBeNull();
    }

    [Fact]
    public async Task An_implausible_offset_is_refused()
    {
        var (_, planRepo, userId) = APlan();

        await Should.ThrowAsync<DomainException>(
            Handler(planRepo, FakeDietAnalyticsRepository.For(userId), userId)
                .Handle(new GetObservationsQuery(PeriodPreset.Month, 20 * 60), CancellationToken.None));
    }

    [Fact]
    public async Task Asking_without_a_signed_in_member_is_refused()
    {
        var handler = Handler(
            FakeDietPlanRepository.Empty(), FakeDietAnalyticsRepository.For(Guid.NewGuid()),
            Guid.NewGuid(), anonymous: true);

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetObservationsQuery(PeriodPreset.Month, 0), CancellationToken.None));
    }

    // --- Helpers ----------------------------------------------------------

    private static SeededEntry At(DateOnly date, int hour, int minute, int calories) =>
        new("Seeded meal", FoodCategory.PreparedMeal, MealType.Dinner, calories,
            date.ToDateTime(new TimeOnly(hour, minute)), Guid.NewGuid());

    private static GetObservationsHandler Handler(
        FakeDietPlanRepository planRepo, FakeDietAnalyticsRepository analytics, Guid userId,
        bool anonymous = false) =>
        new(planRepo,
            analytics,
            new AnalysisPeriodResolver(),
            new IntakeAnalyser(),
            new MacronutrientAnalyser(),
            new PatternAnalyser(),
            new ObservationEngine(ObservationEngineTests.AllRules()),
            anonymous ? SignedInUser.Anonymous() : SignedInUser.WithId(userId));

    private static (DietPlanAggregate Plan, FakeDietPlanRepository Repo, Guid UserId) APlan(int startedDaysAgo = 90)
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(startedDaysAgo).WithTargets(2100);
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }
}
