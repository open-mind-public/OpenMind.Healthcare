using DietApi.Domain.ValueObjects;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// The trend is the payoff for a member whose goal is to lose or gain, so the two figures it
/// reports - change so far, distance left - have to be right in both directions.
/// </summary>
public class WeightTrendTests
{
    [Fact]
    public void A_new_plan_has_a_single_reading_and_no_change_yet()
    {
        var builder = DietPlanBuilder.APlan().Weighing(84.6m).TargetingWeight(78m);
        var plan = builder.Build();

        var trend = plan.WeightTrend(asOf: builder.Clock);

        trend.Readings.Count.ShouldBe(1);
        trend.StartWeightKg.ShouldBe(84.6m);
        trend.CurrentWeightKg.ShouldBe(84.6m);
        trend.ChangeKg.ShouldBe(0m);
        trend.RemainingToTargetKg.ShouldBe(6.6m);
        trend.GoalReached.ShouldBeFalse();
    }

    [Fact]
    public void Losing_weight_reports_a_negative_change_and_a_shrinking_distance()
    {
        var builder = DietPlanBuilder.APlan()
            .WithGoal(GoalType.LoseWeight)
            .StartedDaysAgo(30)
            .Weighing(82.0m)                 // today
            .WeighedDaysAgo(30, 86.0m)       // at the plan start
            .TargetingWeight(78m);
        var plan = builder.Build();

        var trend = plan.WeightTrend(asOf: builder.Clock);

        trend.StartWeightKg.ShouldBe(86.0m);
        trend.CurrentWeightKg.ShouldBe(82.0m);
        trend.ChangeKg.ShouldBe(-4.0m);
        trend.RemainingToTargetKg.ShouldBe(4.0m);
        trend.GoalReached.ShouldBeFalse();
    }

    [Fact]
    public void Passing_a_weight_loss_target_counts_as_reached_not_overshot()
    {
        var builder = DietPlanBuilder.APlan()
            .WithGoal(GoalType.LoseWeight)
            .StartedDaysAgo(60)
            .Weighing(77.0m)
            .WeighedDaysAgo(60, 86.0m)
            .TargetingWeight(78m);
        var plan = builder.Build();

        var trend = plan.WeightTrend(asOf: builder.Clock);

        trend.GoalReached.ShouldBeTrue();
        trend.RemainingToTargetKg.ShouldBe(1.0m);
    }

    [Fact]
    public void A_gain_goal_is_reached_from_the_other_direction()
    {
        var builder = DietPlanBuilder.APlan()
            .WithGoal(GoalType.GainWeight)
            .StartedDaysAgo(60)
            .Weighing(80.0m)
            .WeighedDaysAgo(60, 74.0m)
            .TargetingWeight(78m);
        var plan = builder.Build();

        var trend = plan.WeightTrend(asOf: builder.Clock);

        trend.ChangeKg.ShouldBe(6.0m);
        trend.GoalReached.ShouldBeTrue();
    }

    [Fact]
    public void A_gain_goal_short_of_its_target_is_not_reached()
    {
        var builder = DietPlanBuilder.APlan()
            .WithGoal(GoalType.GainWeight)
            .Weighing(76.0m)
            .TargetingWeight(78m);
        var plan = builder.Build();

        plan.WeightTrend(asOf: builder.Clock).GoalReached.ShouldBeFalse();
    }

    [Fact]
    public void A_plan_with_no_target_weight_reports_no_distance_and_no_goal()
    {
        var builder = DietPlanBuilder.APlan().TargetingWeight(null);
        var plan = builder.Build();

        var trend = plan.WeightTrend(asOf: builder.Clock);

        trend.TargetWeightKg.ShouldBeNull();
        trend.RemainingToTargetKg.ShouldBeNull();
        trend.GoalReached.ShouldBeFalse();
    }

    [Fact]
    public void A_period_with_no_readings_returns_an_empty_chart_rather_than_throwing()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(60);
        var plan = builder.Build();

        var trend = plan.WeightTrend(builder.DaysAgo(50), builder.DaysAgo(40), builder.Clock);

        trend.Readings.ShouldBeEmpty();

        // The headline figures still work - they are not limited to the window being charted.
        trend.CurrentWeightKg.ShouldNotBeNull();
    }

    [Fact]
    public void Readings_come_back_in_date_order()
    {
        var builder = DietPlanBuilder.APlan()
            .StartedDaysAgo(30)
            .WeighedDaysAgo(5, 85.0m)
            .WeighedDaysAgo(20, 86.5m)
            .WeighedDaysAgo(12, 85.8m);
        var plan = builder.Build();

        var dates = plan.WeightTrend(asOf: builder.Clock).Readings.Select(r => r.Date).ToList();

        dates.ShouldBe([.. dates.OrderBy(d => d)]);
    }

    [Fact]
    public void Change_is_measured_from_the_plan_start_not_from_the_window_being_viewed()
    {
        // Scrolling the chart must not change how much progress the member appears to have made.
        var builder = DietPlanBuilder.APlan()
            .WithGoal(GoalType.LoseWeight)
            .StartedDaysAgo(60)
            .Weighing(80.0m)
            .WeighedDaysAgo(60, 86.0m)
            .WeighedDaysAgo(10, 81.0m);
        var plan = builder.Build();

        var wholePlan = plan.WeightTrend(asOf: builder.Clock);
        var lastFortnight = plan.WeightTrend(builder.DaysAgo(14), builder.Today, builder.Clock);

        lastFortnight.Readings.Count.ShouldBeLessThan(wholePlan.Readings.Count);
        lastFortnight.ChangeKg.ShouldBe(wholePlan.ChangeKg);
        lastFortnight.ChangeKg.ShouldBe(-6.0m);
    }
}
