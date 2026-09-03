using DDD.BuildingBlocks;
using DietPlanAggregate = DietApi.Domain.Aggregates.DietPlan;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietAnalytics.GetEatingPatterns;
using DietApi.Tests.TestSupport;
using static DietApi.Tests.TestSupport.FakeDietAnalyticsRepository;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for the eating patterns.
/// </summary>
public class EatingPatternsHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task A_member_sees_seven_weekdays_and_twenty_four_hours()
    {
        var (_, planRepo, userId) = APlan();

        var analytics = FakeDietAnalyticsRepository.For(userId)
            .WithDay(Today, [At(Today, 8, 0, 400), At(Today, 20, 30, 800)]);

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetEatingPatternsQuery(PeriodPreset.Week, 0), CancellationToken.None);

        response.ShouldNotBeNull();
        response.ByWeekday.Count.ShouldBe(7);
        response.ByHour.Count.ShouldBe(24);
        response.ByHour.Sum(h => h.Kilocalories).ShouldBe(1200);
    }

    [Fact]
    public async Task The_offset_is_applied_to_the_hourly_distribution()
    {
        var (_, planRepo, userId) = APlan();

        // 14:00 UTC becomes 19:30 at +05:30.
        var analytics = FakeDietAnalyticsRepository.For(userId)
            .WithDay(Today, [At(Today, 14, 0, 500)]);

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetEatingPatternsQuery(PeriodPreset.Week, (5 * 60) + 30), CancellationToken.None);

        response.ShouldNotBeNull();
        response.UtcOffsetMinutes.ShouldBe(330);
        response.ByHour.Single(h => h.Hour == 19).Kilocalories.ShouldBe(500);
    }

    [Fact]
    public async Task A_missing_offset_defaults_to_UTC_rather_than_failing()
    {
        var (_, planRepo, userId) = APlan();

        var analytics = FakeDietAnalyticsRepository.For(userId)
            .WithDay(Today, [At(Today, 9, 0, 300)]);

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetEatingPatternsQuery(PeriodPreset.Week, 0), CancellationToken.None);

        response.ShouldNotBeNull();
        response.UtcOffsetMinutes.ShouldBe(0);
        response.ByHour.Single(h => h.Hour == 9).Kilocalories.ShouldBe(300);
    }

    [Theory]
    [InlineData(-13 * 60)]
    [InlineData(15 * 60)]
    [InlineData(100_000)]
    public async Task An_offset_that_is_not_a_real_time_zone_is_refused(int offsetMinutes)
    {
        // Applying it silently would draw a plausible-looking chart of nonsense.
        var (_, planRepo, userId) = APlan();

        await Should.ThrowAsync<DomainException>(
            Handler(planRepo, FakeDietAnalyticsRepository.For(userId), userId)
                .Handle(new GetEatingPatternsQuery(PeriodPreset.Week, offsetMinutes), CancellationToken.None));
    }

    [Fact]
    public async Task The_approximation_is_stated_in_the_response()
    {
        var (_, planRepo, userId) = APlan();

        var response = await Handler(planRepo, FakeDietAnalyticsRepository.For(userId), userId)
            .Handle(new GetEatingPatternsQuery(PeriodPreset.Week, 0), CancellationToken.None);

        response.ShouldNotBeNull();
        response.IsApproximate.ShouldBeTrue();
        response.ApproximationReason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task A_period_with_nothing_logged_still_returns_a_full_shape()
    {
        var (_, planRepo, userId) = APlan();

        var response = await Handler(planRepo, FakeDietAnalyticsRepository.For(userId), userId)
            .Handle(new GetEatingPatternsQuery(PeriodPreset.Month, 0), CancellationToken.None);

        response.ShouldNotBeNull();
        response.ByWeekday.Count.ShouldBe(7);
        response.ByHour.Count.ShouldBe(24);
        response.ByWeekday.ShouldAllBe(d => d.LoggedDays == 0);
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = Handler(
            FakeDietPlanRepository.Empty(), FakeDietAnalyticsRepository.For(Guid.NewGuid()), Guid.NewGuid());

        (await handler.Handle(new GetEatingPatternsQuery(PeriodPreset.Month, 0), CancellationToken.None))
            .ShouldBeNull();
    }

    [Fact]
    public async Task Asking_without_a_signed_in_member_is_refused()
    {
        var handler = new GetEatingPatternsHandler(
            FakeDietPlanRepository.Empty(),
            FakeDietAnalyticsRepository.For(Guid.NewGuid()),
            new AnalysisPeriodResolver(),
            new PatternAnalyser(),
            SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetEatingPatternsQuery(PeriodPreset.Month, 0), CancellationToken.None));
    }

    // --- Helpers ----------------------------------------------------------

    private static SeededEntry At(DateOnly date, int hour, int minute, int calories) =>
        new("Seeded meal", FoodCategory.PreparedMeal, MealType.Dinner, calories,
            date.ToDateTime(new TimeOnly(hour, minute)), Guid.NewGuid());

    private static GetEatingPatternsHandler Handler(
        FakeDietPlanRepository planRepo, FakeDietAnalyticsRepository analytics, Guid userId) =>
        new(planRepo, analytics, new AnalysisPeriodResolver(), new PatternAnalyser(),
            SignedInUser.WithId(userId));

    private static (DietPlanAggregate Plan, FakeDietPlanRepository Repo, Guid UserId) APlan(int startedDaysAgo = 60)
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(startedDaysAgo).WithTargets(2100);
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }
}
