using System.Reflection;
using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietAnalytics.GetEatingPatterns;
using DietApi.Features.DietAnalytics.GetIntakeAnalysis;
using DietApi.Features.DietAnalytics.GetMacroAnalysis;
using DietApi.Features.DietAnalytics.GetObservations;
using DietApi.Tests.Domain;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Viewing analytics changes nothing.
/// </summary>
/// <remarks>
/// Derived figures are safe precisely because they are derived. The moment analytics writes
/// anything — a cached total, a "last viewed" stamp, a recomputed target — it becomes something
/// that can disagree with the log it claims to describe, and a member studying a page that
/// silently edits their history has no way to tell.
/// </remarks>
public class AnalyticsAreReadOnlyTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Running_every_analytics_read_leaves_the_plan_exactly_as_it_was()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(90).WithTargets(2100);
        var plan = builder.Build();
        var planRepo = FakeDietPlanRepository.Containing(plan);
        var userId = builder.UserId;

        var before = Snapshot(plan);

        await RunEveryRead(planRepo, Seeded(userId), userId);

        Snapshot(plan).ShouldBe(before);
        planRepo.SaveCount.ShouldBe(0, "analytics must never persist anything");
    }

    [Fact]
    public async Task Running_every_analytics_read_leaves_each_logged_day_unchanged()
    {
        var planBuilder = DietPlanBuilder.APlan().StartedDaysAgo(90).WithTargets(2100);
        var plan = planBuilder.Build();
        var userId = planBuilder.UserId;

        // Real logged days, alongside the read model the handlers use.
        var days = new List<LoggedDay>();
        for (var i = 0; i < 20; i++)
        {
            days.Add(LoggedDayBuilder.ADay()
                .ForUser(userId).ForPlan(plan.Id).DaysAgo(i).Targeting(2100)
                .Ate(FakeFoodLibraryRepository.Oats(), quantity: i % 3 == 0 ? 12m : 4m)
                .Build());
        }

        var before = days.Select(DaySnapshot).ToList();

        await RunEveryRead(FakeDietPlanRepository.Containing(plan), Seeded(userId), userId);

        days.Select(DaySnapshot).ShouldBe(before);
    }

    [Fact]
    public void The_read_model_offers_no_way_to_write()
    {
        // Every method on the analytics repository is a query. There is no Add, Update or Delete
        // to call, so a handler could not persist through it even by mistake.
        foreach (var method in typeof(IDietAnalyticsRepository).GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            method.Name.ShouldStartWith("Get");
        }
    }

    // --- Helpers ----------------------------------------------------------

    private static async Task RunEveryRead(
        FakeDietPlanRepository planRepo, FakeDietAnalyticsRepository analytics, Guid userId)
    {
        var resolver = new AnalysisPeriodResolver();
        var intake = new IntakeAnalyser();
        var macros = new MacronutrientAnalyser();
        var patterns = new PatternAnalyser();
        var user = SignedInUser.WithId(userId);

        await new GetIntakeAnalysisHandler(planRepo, analytics, resolver, intake, user)
            .Handle(new GetIntakeAnalysisQuery(PeriodPreset.Month), CancellationToken.None);

        await new GetMacroAnalysisHandler(planRepo, analytics, resolver, macros, user)
            .Handle(new GetMacroAnalysisQuery(PeriodPreset.Month), CancellationToken.None);

        await new GetEatingPatternsHandler(planRepo, analytics, resolver, patterns, user)
            .Handle(new GetEatingPatternsQuery(PeriodPreset.Month, 0), CancellationToken.None);

        await new GetObservationsHandler(
                planRepo, analytics, resolver, intake, macros, patterns,
                new ObservationEngine(ObservationEngineTests.AllRules()), user)
            .Handle(new GetObservationsQuery(PeriodPreset.Month, 0), CancellationToken.None);
    }

    private static FakeDietAnalyticsRepository Seeded(Guid userId)
    {
        var analytics = FakeDietAnalyticsRepository.For(userId);

        for (var i = 0; i < 20; i++)
        {
            var date = Today.AddDays(-i);
            analytics.WithDay(date,
            [
                new FakeDietAnalyticsRepository.SeededEntry(
                    "Porridge oats", FoodCategory.Staple, MealType.Breakfast, 400,
                    date.ToDateTime(new TimeOnly(8, 0)), Guid.NewGuid()),
                new FakeDietAnalyticsRepository.SeededEntry(
                    "Dinner", FoodCategory.PreparedMeal, MealType.Dinner, 1600,
                    date.ToDateTime(new TimeOnly(22, 0)), Guid.NewGuid())
            ]);
        }

        return analytics;
    }

    private static string Snapshot(DietPlan plan) =>
        string.Join('|',
            plan.Targets.Calories, plan.Targets.ProteinG, plan.Targets.CarbsG, plan.Targets.FatG,
            plan.TargetSource, plan.ActivityLevel, plan.Goal, plan.StartDate,
            plan.CurrentWeightKg(), plan.WeightReadings.Count, plan.UpdatedAt.Ticks);

    private static string DaySnapshot(LoggedDay day) =>
        string.Join('|',
            day.Date, day.TargetSnapshot.Calories, day.Totals.Calories,
            day.Assess().State, day.Version, day.Entries.Count, day.UpdatedAt.Ticks);
}
