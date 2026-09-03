using DietPlanAggregate = DietApi.Domain.Aggregates.DietPlan;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietAnalytics.GetIntakeAnalysis;
using DietApi.Tests.TestSupport;
using static DietApi.Tests.TestSupport.FakeDietAnalyticsRepository;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for the intake analysis.
/// </summary>
public class IntakeAnalysisHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task A_member_with_history_sees_totals_breakdowns_and_top_foods()
    {
        var (plan, planRepo, userId) = APlan();
        var oats = Guid.NewGuid();

        var analytics = FakeDietAnalyticsRepository.For(userId)
            .WithDay(Today, [Entry("Porridge oats", FoodCategory.Staple, MealType.Breakfast, 400, Today, oats),
                             Entry("Chicken salad", FoodCategory.Protein, MealType.Lunch, 600, Today)])
            .WithDay(Today.AddDays(-1), [Entry("Porridge oats", FoodCategory.Staple, MealType.Breakfast, 400, Today.AddDays(-1), oats)]);

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetIntakeAnalysisQuery(PeriodPreset.Week), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Summary.TotalKilocalories.ShouldBe(1400);
        response.Summary.AveragedOverDays.ShouldBe(2);
        response.Summary.AverageDailyKilocalories.ShouldBe(700);

        // Oats appear twice and outrank the single larger entry.
        response.TopFoods[0].FoodName.ShouldBe("Porridge oats");
        response.TopFoods[0].TimesLogged.ShouldBe(2);
        plan.ShouldNotBeNull();
    }

    [Fact]
    public async Task The_meal_and_category_breakdowns_reconcile_with_the_total()
    {
        var (_, planRepo, userId) = APlan();

        var analytics = FakeDietAnalyticsRepository.For(userId)
            .WithDay(Today, [Entry("A", FoodCategory.Staple, MealType.Breakfast, 500, Today),
                             Entry("B", FoodCategory.Fruit, MealType.Snack, 250, Today),
                             Entry("C", FoodCategory.Protein, MealType.Dinner, 750, Today)]);

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetIntakeAnalysisQuery(PeriodPreset.Week), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Meals.Sum(m => m.Kilocalories).ShouldBe(response.Summary.TotalKilocalories);
        response.Categories.Sum(c => c.Kilocalories).ShouldBe(response.Summary.TotalKilocalories);
        response.Meals.Sum(m => m.ShareOfTotal).ShouldBe(100m);
        response.Categories.Sum(c => c.ShareOfTotal).ShouldBe(100m);
    }

    [Fact]
    public async Task The_day_state_split_covers_the_whole_period_not_just_the_logged_days()
    {
        var (_, planRepo, userId) = APlan();

        var analytics = FakeDietAnalyticsRepository.For(userId)
            .WithSimpleDay(Today, calories: 1800)
            .WithSimpleDay(Today.AddDays(-1), calories: 2600);

        var response = await Handler(planRepo, analytics, userId)
            .Handle(new GetIntakeAnalysisQuery(PeriodPreset.Week), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Summary.OnTargetDays.ShouldBe(1);
        response.Summary.OverTargetDays.ShouldBe(1);
        response.Summary.NotLoggedDays.ShouldBe(5);
        (response.Summary.OnTargetDays + response.Summary.OverTargetDays + response.Summary.NotLoggedDays)
            .ShouldBe(response.Period.TotalDays);
    }

    [Fact]
    public async Task A_member_with_a_plan_and_nothing_logged_gets_zeros_rather_than_an_error()
    {
        var (_, planRepo, userId) = APlan();

        var response = await Handler(planRepo, FakeDietAnalyticsRepository.For(userId), userId)
            .Handle(new GetIntakeAnalysisQuery(PeriodPreset.Month), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Summary.TotalKilocalories.ShouldBe(0);
        response.Summary.AveragedOverDays.ShouldBe(0);
        response.Summary.NotLoggedDays.ShouldBe(response.Period.TotalDays);
        response.Meals.Count.ShouldBe(Enum.GetValues<MealType>().Length);
        response.TopFoods.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = Handler(
            FakeDietPlanRepository.Empty(), FakeDietAnalyticsRepository.For(Guid.NewGuid()), Guid.NewGuid());

        (await handler.Handle(new GetIntakeAnalysisQuery(PeriodPreset.Month), CancellationToken.None))
            .ShouldBeNull();
    }

    [Fact]
    public async Task Another_members_days_are_not_counted()
    {
        var (_, planRepo, userId) = APlan();

        // The fake answers only for the member it was built for, as the real queries arrange.
        var someoneElses = FakeDietAnalyticsRepository.For(Guid.NewGuid())
            .WithSimpleDay(Today, calories: 3000);

        var response = await Handler(planRepo, someoneElses, userId)
            .Handle(new GetIntakeAnalysisQuery(PeriodPreset.Week), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Summary.TotalKilocalories.ShouldBe(0);
    }

    [Fact]
    public async Task Asking_without_a_signed_in_member_is_refused()
    {
        var handler = new GetIntakeAnalysisHandler(
            FakeDietPlanRepository.Empty(),
            FakeDietAnalyticsRepository.For(Guid.NewGuid()),
            new AnalysisPeriodResolver(),
            new IntakeAnalyser(),
            SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetIntakeAnalysisQuery(PeriodPreset.Month), CancellationToken.None));
    }

    [Fact]
    public async Task A_period_longer_than_the_plan_is_narrowed_and_says_so()
    {
        var (_, planRepo, userId) = APlan(startedDaysAgo: 3);

        var response = await Handler(planRepo, FakeDietAnalyticsRepository.For(userId), userId)
            .Handle(new GetIntakeAnalysisQuery(PeriodPreset.Quarter), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Period.WasNarrowed.ShouldBeTrue();
        response.Period.TotalDays.ShouldBe(4);
    }

    // --- Helpers ----------------------------------------------------------

    private static SeededEntry Entry(
        string name, FoodCategory category, MealType meal, int calories, DateOnly date, Guid? id = null) =>
        new(name, category, meal, calories, date.ToDateTime(new TimeOnly(12, 0)), id ?? Guid.NewGuid());

    private static GetIntakeAnalysisHandler Handler(
        FakeDietPlanRepository planRepo, FakeDietAnalyticsRepository analytics, Guid userId) =>
        new(planRepo, analytics, new AnalysisPeriodResolver(), new IntakeAnalyser(), SignedInUser.WithId(userId));

    private static (DietPlanAggregate Plan, FakeDietPlanRepository Repo, Guid UserId) APlan(int startedDaysAgo = 60)
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(startedDaysAgo).WithTargets(2100);
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }
}
