using DDD.BuildingBlocks;
using DietApi.Domain.Rules;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// A date carries one weight reading, never two, and a plan never loses its last one - the
/// suggested target is calculated from current weight, so that value must always have a source.
/// </summary>
public class WeightRecordingTests
{
    [Fact]
    public void A_new_plan_starts_with_the_weight_supplied_at_setup()
    {
        var builder = DietPlanBuilder.APlan().Weighing(84.6m);
        var plan = builder.Build();

        plan.WeightReadings.Count.ShouldBe(1);
        plan.CurrentWeightKg(builder.Clock).ShouldBe(84.6m);
    }

    [Fact]
    public void A_second_reading_on_the_same_date_replaces_the_first()
    {
        var builder = DietPlanBuilder.APlan().Weighing(84.6m);
        var plan = builder.Build();

        plan.RecordWeight(builder.Today, 83.2m, builder.Clock);

        plan.WeightReadings.Count(r => r.Date == builder.Today).ShouldBe(1);
        plan.CurrentWeightKg(builder.Clock).ShouldBe(83.2m);
    }

    [Fact]
    public void Current_weight_is_the_most_recent_reading_at_or_before_the_moment_asked_about()
    {
        var builder = DietPlanBuilder.APlan()
            .Weighing(84.6m)
            .WeighedDaysAgo(10, 86.0m)
            .WeighedDaysAgo(3, 85.1m);
        var plan = builder.Build();

        // Today's setup reading is the newest of the three.
        plan.CurrentWeightKg(builder.Clock).ShouldBe(84.6m);

        // Asked about a moment before it, the newest reading at or before that date wins.
        plan.CurrentWeightKg(builder.Clock.AddDays(-5)).ShouldBe(86.0m);
    }

    [Fact]
    public void Readings_can_be_removed_while_more_than_one_remains()
    {
        var builder = DietPlanBuilder.APlan().WeighedDaysAgo(5, 85.0m);
        var plan = builder.Build();

        plan.RemoveWeightReading(builder.DaysAgo(5)).ShouldBeTrue();
        plan.WeightReadings.Count.ShouldBe(1);
    }

    [Fact]
    public void Removing_a_date_with_no_reading_reports_nothing_removed()
    {
        var builder = DietPlanBuilder.APlan();
        var plan = builder.Build();

        plan.RemoveWeightReading(builder.DaysAgo(99)).ShouldBeFalse();
        plan.WeightReadings.Count.ShouldBe(1);
    }

    [Fact]
    public void The_only_remaining_reading_cannot_be_removed()
    {
        var builder = DietPlanBuilder.APlan();
        var plan = builder.Build();

        var act = () => { plan.RemoveWeightReading(builder.Today); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(CannotRemoveLastWeightReadingRule));
        plan.WeightReadings.Count.ShouldBe(1);
    }

    [Fact]
    public void Current_weight_survives_removing_every_reading_but_one()
    {
        var builder = DietPlanBuilder.APlan()
            .Weighing(84.6m)
            .WeighedDaysAgo(10, 86.0m)
            .WeighedDaysAgo(3, 85.1m);
        var plan = builder.Build();

        plan.RemoveWeightReading(builder.DaysAgo(10));
        plan.RemoveWeightReading(builder.DaysAgo(3));

        Should.NotThrow(() => plan.CurrentWeightKg(builder.Clock));
        plan.CurrentWeightKg(builder.Clock).ShouldBe(84.6m);
    }
}
