using DDD.BuildingBlocks;
using DietApi.Domain.Rules;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// A shortcut cannot hold a session that could never be recorded.
/// </summary>
/// <remarks>
/// The aggregate reuses the very same rule objects that guard a recorded session rather than
/// copying their thresholds, so this is not two implementations that happen to agree today. The
/// boundary values here are the ones the exercise entry rules are tested at in 002; if the two ever
/// drift apart, both files fail together.
/// </remarks>
public class ShortcutDurationRulesTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-45)]
    public void A_shortcut_must_record_some_time(int minutes)
    {
        var plan = DietPlanBuilder.APlan().Build();

        var act = () => { plan.SaveExerciseShortcut(Guid.NewGuid(), minutes, "Nothing at all"); };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(DurationMustBePositiveRule));
    }

    [Fact]
    public void One_minute_is_allowed_on_a_shortcut_exactly_as_on_a_session()
    {
        var plan = DietPlanBuilder.APlan().Build();

        plan.SaveExerciseShortcut(Guid.NewGuid(), 1, "A quick one").DurationMinutes.ShouldBe(1);
    }

    [Fact]
    public void Exactly_the_ceiling_is_allowed()
    {
        var plan = DietPlanBuilder.APlan().Build();

        var shortcut = plan.SaveExerciseShortcut(
            Guid.NewGuid(), DurationWithinCeilingRule.CeilingMinutes, "All day");

        shortcut.DurationMinutes.ShouldBe(DurationWithinCeilingRule.CeilingMinutes);
    }

    [Fact]
    public void One_minute_past_the_ceiling_is_refused()
    {
        var plan = DietPlanBuilder.APlan().Build();

        var act = () =>
        {
            plan.SaveExerciseShortcut(
                Guid.NewGuid(), DurationWithinCeilingRule.CeilingMinutes + 1, "Longer than a day");
        };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(DurationWithinCeilingRule));
    }
}
