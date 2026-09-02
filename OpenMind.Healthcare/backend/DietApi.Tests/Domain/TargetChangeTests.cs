using DietApi.Domain.ValueObjects;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// Changing body details offers a refreshed suggestion; it never overwrites a target the member
/// chose. Only <c>SetTargets</c> moves the number that is actually in force.
/// </summary>
public class TargetChangeTests
{
    [Fact]
    public void Updating_the_plan_leaves_the_targets_in_force_untouched()
    {
        var builder = DietPlanBuilder.APlan().WithTargets(2100, TargetSource.MemberSet);
        var plan = builder.Build();

        plan.UpdatePlan(
            GoalType.Maintain,
            builder.DaysAgo(10),
            BodyMetrics.Create(180m, 35, BiologicalSex.Male),
            ActivityLevel.VeryActive,
            targetWeightKg: 80m,
            asOf: builder.Clock);

        plan.Targets.Calories.ShouldBe(2100);
        plan.TargetSource.ShouldBe(TargetSource.MemberSet);

        // Everything else did change.
        plan.Goal.ShouldBe(GoalType.Maintain);
        plan.ActivityLevel.ShouldBe(ActivityLevel.VeryActive);
        plan.BodyMetrics.HeightCm.ShouldBe(180m);
    }

    [Fact]
    public void Recording_a_new_weight_does_not_move_the_target()
    {
        var builder = DietPlanBuilder.APlan().WithTargets(2100, TargetSource.MemberSet);
        var plan = builder.Build();

        plan.RecordWeight(builder.Today, 79.0m, builder.Clock);

        plan.Targets.Calories.ShouldBe(2100);
        plan.TargetSource.ShouldBe(TargetSource.MemberSet);
    }

    [Fact]
    public void Setting_targets_records_that_the_member_chose_them()
    {
        var plan = DietPlanBuilder.APlan().WithTargets(2100).Build();

        plan.SetTargets(NutritionTargets.Create(1800), TargetSource.MemberSet);

        plan.Targets.Calories.ShouldBe(1800);
        plan.TargetSource.ShouldBe(TargetSource.MemberSet);
    }

    [Fact]
    public void Accepting_a_suggestion_records_that_it_came_from_the_system()
    {
        var plan = DietPlanBuilder.APlan().WithTargets(2100, TargetSource.MemberSet).Build();

        plan.SetTargets(NutritionTargets.Create(2400), TargetSource.Suggested);

        plan.TargetSource.ShouldBe(TargetSource.Suggested);
    }

    [Fact]
    public void A_member_may_set_a_target_below_the_safe_floor()
    {
        // The floor clamps what the system suggests. It does not veto the member's own choice -
        // the warning is the interface's job, not a refusal.
        var plan = DietPlanBuilder.APlan().Build();

        plan.SetTargets(NutritionTargets.Create(900), TargetSource.MemberSet);

        plan.Targets.Calories.ShouldBe(900);
    }
}
