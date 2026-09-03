using DietPlanAggregate = DietApi.Domain.Aggregates.DietPlan;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietAnalytics.GetMacroAnalysis;
using DietApi.Tests.TestSupport;
using static DietApi.Tests.TestSupport.FakeDietAnalyticsRepository;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for the macronutrient analysis.
/// </summary>
public class MacroAnalysisHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task A_member_sees_their_split_against_the_targets_in_force()
    {
        var (_, planRepo, userId) = APlan();

        var analytics = FakeDietAnalyticsRepository.For(userId)
            .WithDay(Today, [Meal(600, Today)], proteinG: 120m, carbsG: 200m, fatG: 70m)
            .WithDay(Today.AddDays(-1), [Meal(600, Today.AddDays(-1))], proteinG: 140m, carbsG: 220m, fatG: 80m);

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetMacroAnalysisQuery(PeriodPreset.Week), CancellationToken.None);

        response.ShouldNotBeNull();
        response.HasTargets.ShouldBeTrue();
        response.Actual.ProteinG.ShouldBe(130m);
        response.Target.ShouldNotBeNull();
        response.Target.ProteinG.ShouldBe(157.5m);
        response.AveragedOverDays.ShouldBe(2);
    }

    [Fact]
    public async Task A_period_spanning_a_target_change_reports_the_average_of_the_stored_targets()
    {
        var (_, planRepo, userId) = APlan();

        var analytics = FakeDietAnalyticsRepository.For(userId)
            .WithDay(Today, [Meal(600, Today)], targetProteinG: 120m)
            .WithDay(Today.AddDays(-1), [Meal(600, Today.AddDays(-1))], targetProteinG: 160m);

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetMacroAnalysisQuery(PeriodPreset.Week), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Target.ShouldNotBeNull();
        response.Target.ProteinG.ShouldBe(140m);
    }

    [Fact]
    public async Task A_plan_with_no_macronutrient_targets_gets_a_split_and_a_null_target()
    {
        var (_, planRepo, userId) = APlan();

        var analytics = FakeDietAnalyticsRepository.For(userId)
            .WithDay(Today, [Meal(600, Today)],
                targetProteinG: null, targetCarbsG: null, targetFatG: null);

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetMacroAnalysisQuery(PeriodPreset.Week), CancellationToken.None);

        response.ShouldNotBeNull();
        response.HasTargets.ShouldBeFalse();
        response.Target.ShouldBeNull();
        response.Actual.ProteinG.ShouldBeGreaterThan(0m);
        response.ShareOfEnergy.Protein.ShouldBeGreaterThan(0m);
    }

    [Fact]
    public async Task A_period_with_nothing_logged_gives_zeros_rather_than_an_error()
    {
        var (_, planRepo, userId) = APlan();

        var response = await Handler(planRepo, FakeDietAnalyticsRepository.For(userId), userId)
            .Handle(new GetMacroAnalysisQuery(PeriodPreset.Month), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Actual.ProteinG.ShouldBe(0m);
        response.AveragedOverDays.ShouldBe(0);
        response.HasTargets.ShouldBeFalse();
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = Handler(
            FakeDietPlanRepository.Empty(), FakeDietAnalyticsRepository.For(Guid.NewGuid()), Guid.NewGuid());

        (await handler.Handle(new GetMacroAnalysisQuery(PeriodPreset.Month), CancellationToken.None))
            .ShouldBeNull();
    }

    [Fact]
    public async Task Asking_without_a_signed_in_member_is_refused()
    {
        var handler = new GetMacroAnalysisHandler(
            FakeDietPlanRepository.Empty(),
            FakeDietAnalyticsRepository.For(Guid.NewGuid()),
            new AnalysisPeriodResolver(),
            new MacronutrientAnalyser(),
            SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetMacroAnalysisQuery(PeriodPreset.Month), CancellationToken.None));
    }

    // --- Helpers ----------------------------------------------------------

    private static SeededEntry Meal(int calories, DateOnly date) =>
        new("Seeded meal", FoodCategory.PreparedMeal, MealType.Dinner, calories,
            date.ToDateTime(new TimeOnly(19, 0)), Guid.NewGuid());

    private static GetMacroAnalysisHandler Handler(
        FakeDietPlanRepository planRepo, FakeDietAnalyticsRepository analytics, Guid userId) =>
        new(planRepo, analytics, new AnalysisPeriodResolver(), new MacronutrientAnalyser(),
            SignedInUser.WithId(userId));

    private static (DietPlanAggregate Plan, FakeDietPlanRepository Repo, Guid UserId) APlan(int startedDaysAgo = 60)
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(startedDaysAgo).WithTargets(2100);
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }
}
