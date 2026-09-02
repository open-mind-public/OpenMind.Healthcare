using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.Domain;

/// <summary>
/// The suggestion is the one calculation in this feature a member is asked to trust, so it is
/// pinned at every branch: both sexes, all five activity levels, all four goals, and the floor.
/// </summary>
public class TargetSuggestionTests
{
    private readonly TargetSuggestionService _service = new();

    [Fact]
    public void A_moderately_active_man_losing_weight_gets_the_expected_target()
    {
        // Mifflin-St Jeor: 10(84.6) + 6.25(178) - 5(34) + 5 = 1793.5 -> 1794
        // Activity 1.55 -> 2781. Goal -500 -> 2281. Above the 1500 floor.
        var suggestion = _service.Suggest(Body(BiologicalSex.Male), 84.6m, ActivityLevel.ModeratelyActive, GoalType.LoseWeight);

        suggestion.RestingEnergyKcal.ShouldBe(1794);
        suggestion.ActivityAdjustedKcal.ShouldBe(2781);
        suggestion.GoalAdjustmentKcal.ShouldBe(-500);
        suggestion.SuggestedTargets.Calories.ShouldBe(2281);
        suggestion.WasClampedToFloor.ShouldBeFalse();
    }

    [Fact]
    public void The_female_constant_differs_from_the_male_one()
    {
        var female = _service.Suggest(Body(BiologicalSex.Female), 84.6m, ActivityLevel.Sedentary, GoalType.Maintain);
        var male = _service.Suggest(Body(BiologicalSex.Male), 84.6m, ActivityLevel.Sedentary, GoalType.Maintain);

        // 5 versus -161 is a 166 kcal difference in resting energy.
        (male.RestingEnergyKcal - female.RestingEnergyKcal).ShouldBe(166);
    }

    [Theory]
    [InlineData(ActivityLevel.Sedentary, 1.2)]
    [InlineData(ActivityLevel.LightlyActive, 1.375)]
    [InlineData(ActivityLevel.ModeratelyActive, 1.55)]
    [InlineData(ActivityLevel.VeryActive, 1.725)]
    [InlineData(ActivityLevel.ExtraActive, 1.9)]
    public void Each_activity_level_applies_its_own_factor(ActivityLevel level, double factor)
    {
        var suggestion = _service.Suggest(Body(BiologicalSex.Male), 84.6m, level, GoalType.Maintain);

        var expected = (int)Math.Round(suggestion.RestingEnergyKcal * (decimal)factor, MidpointRounding.AwayFromZero);
        suggestion.ActivityAdjustedKcal.ShouldBe(expected);
    }

    [Theory]
    [InlineData(GoalType.LoseWeight, -500)]
    [InlineData(GoalType.Maintain, 0)]
    [InlineData(GoalType.GainWeight, 400)]
    [InlineData(GoalType.EatConsistently, 0)]
    public void Each_goal_applies_its_own_adjustment(GoalType goal, int adjustment)
    {
        var suggestion = _service.Suggest(Body(BiologicalSex.Male), 84.6m, ActivityLevel.ModeratelyActive, goal);

        suggestion.GoalAdjustmentKcal.ShouldBe(adjustment);
        suggestion.SuggestedTargets.Calories.ShouldBe(suggestion.ActivityAdjustedKcal + adjustment);
    }

    [Fact]
    public void A_suggestion_is_never_returned_below_the_floor()
    {
        // A small, older, sedentary woman losing weight computes below 1200 before clamping.
        var suggestion = _service.Suggest(
            BodyMetrics.Create(150m, 70, BiologicalSex.Female), 40m, ActivityLevel.Sedentary, GoalType.LoseWeight);

        suggestion.WasClampedToFloor.ShouldBeTrue();
        suggestion.FloorKcal.ShouldBe(TargetSuggestionService.FemaleFloorKcal);
        suggestion.SuggestedTargets.Calories.ShouldBe(TargetSuggestionService.FemaleFloorKcal);

        // The unclamped figures are still reported honestly, so the interface can explain itself.
        (suggestion.ActivityAdjustedKcal + suggestion.GoalAdjustmentKcal)
            .ShouldBeLessThan(TargetSuggestionService.FemaleFloorKcal);
    }

    [Fact]
    public void Each_sex_carries_its_own_floor()
    {
        TargetSuggestionService.FloorFor(BiologicalSex.Female).ShouldBe(1200);
        TargetSuggestionService.FloorFor(BiologicalSex.Male).ShouldBe(1500);
    }

    [Fact]
    public void An_unclamped_suggestion_reports_no_clamping()
    {
        var suggestion = _service.Suggest(Body(BiologicalSex.Male), 84.6m, ActivityLevel.VeryActive, GoalType.Maintain);

        suggestion.WasClampedToFloor.ShouldBeFalse();
        suggestion.SuggestedTargets.Calories.ShouldBeGreaterThan(suggestion.FloorKcal);
    }

    [Fact]
    public void Macronutrient_grams_add_back_up_to_the_calorie_target()
    {
        var suggestion = _service.Suggest(Body(BiologicalSex.Male), 84.6m, ActivityLevel.ModeratelyActive, GoalType.LoseWeight);
        var targets = suggestion.SuggestedTargets;

        var fromMacros = (targets.ProteinG!.Value * 4m) + (targets.CarbsG!.Value * 4m) + (targets.FatG!.Value * 9m);

        // Rounding each gram figure to one decimal place moves the total by a few calories at most.
        fromMacros.ShouldBe(targets.Calories, tolerance: 5m);
    }

    private static BodyMetrics Body(BiologicalSex sex) => BodyMetrics.Create(178m, 34, sex);
}
