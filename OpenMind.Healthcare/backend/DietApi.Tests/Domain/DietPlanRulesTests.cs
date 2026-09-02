using DDD.BuildingBlocks;
using DietApi.Domain.Aggregates;
using DietApi.Domain.Rules;
using DietApi.Domain.ValueObjects;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// Every business rule guarding a diet plan, proven to break when it should and to pass at its
/// boundary values.
/// </summary>
public class DietPlanRulesTests
{
    private static readonly DateTime Clock = new(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Clock);

    [Fact]
    public void A_plan_cannot_start_in_the_future()
    {
        var act = () => { Plan(startDate: Today.AddDays(1)); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(PlanStartDateCannotBeInFutureRule));
    }

    [Fact]
    public void A_plan_starting_today_is_allowed()
    {
        var plan = Plan(startDate: Today);

        plan.StartDate.ShouldBe(Today);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-2100)]
    public void A_daily_calorie_target_must_be_positive(int calories)
    {
        new DailyCalorieTargetMustBePositiveRule(calories).IsBroken().ShouldBeTrue();

        // The value object guards the same invariant before the aggregate is ever reached.
        Should.Throw<DomainException>(() => NutritionTargets.Create(calories));
    }

    [Fact]
    public void A_daily_calorie_target_of_one_is_allowed()
    {
        new DailyCalorieTargetMustBePositiveRule(1).IsBroken().ShouldBeFalse();
    }

    [Theory]
    [InlineData(49)]
    [InlineData(251)]
    public void Height_outside_the_plausible_range_is_refused(decimal heightCm)
    {
        var act = () => { Plan(heightCm: heightCm); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(HeightMustBePlausibleRule));
    }

    [Theory]
    [InlineData(50)]
    [InlineData(250)]
    public void Height_at_the_boundary_is_allowed(decimal heightCm)
    {
        Plan(heightCm: heightCm).BodyMetrics.HeightCm.ShouldBe(heightCm);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(121)]
    public void Age_outside_the_plausible_range_is_refused(int age)
    {
        var act = () => { Plan(age: age); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(AgeMustBePlausibleRule));
    }

    [Theory]
    [InlineData(13)]
    [InlineData(120)]
    public void Age_at_the_boundary_is_allowed(int age)
    {
        Plan(age: age).BodyMetrics.Age.ShouldBe(age);
    }

    [Theory]
    [InlineData(19)]
    [InlineData(501)]
    public void A_target_weight_outside_the_plausible_range_is_refused(decimal targetWeightKg)
    {
        var act = () => { Plan(targetWeightKg: targetWeightKg); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(TargetWeightMustBePlausibleRule));
    }

    [Fact]
    public void A_plan_without_a_target_weight_is_allowed()
    {
        Plan(targetWeightKg: null).TargetWeightKg.ShouldBeNull();
    }

    [Theory]
    [InlineData(19)]
    [InlineData(501)]
    public void A_current_weight_outside_the_plausible_range_is_refused(decimal weightKg)
    {
        var act = () => { Plan(currentWeightKg: weightKg); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(WeightMustBePlausibleRule));
    }

    [Fact]
    public void A_weight_reading_cannot_be_dated_in_the_future()
    {
        var plan = DietPlanBuilder.APlan().Build();

        var act = () => { plan.RecordWeight(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), 80m, DateTime.UtcNow); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(WeightDateCannotBeInFutureRule));
    }

    [Fact]
    public void A_plan_must_belong_to_a_member()
    {
        var act = () => { Plan(userId: Guid.Empty); };

        act.ShouldThrow<DomainException>();
    }

    private static DietPlan Plan(
        Guid? userId = null,
        DateOnly? startDate = null,
        decimal heightCm = 178m,
        int age = 34,
        decimal currentWeightKg = 84.6m,
        decimal? targetWeightKg = 78m) =>
        DietPlan.Create(
            userId ?? Guid.NewGuid(),
            GoalType.LoseWeight,
            startDate ?? Today.AddDays(-30),
            BodyMetrics.Create(heightCm, age, BiologicalSex.Male),
            ActivityLevel.ModeratelyActive,
            NutritionTargets.Create(2100),
            TargetSource.Suggested,
            currentWeightKg,
            targetWeightKg,
            Clock);
}
