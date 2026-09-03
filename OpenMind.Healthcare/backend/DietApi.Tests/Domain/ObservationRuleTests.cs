using DietApi.Domain.Observations;
using DietApi.Domain.Observations.Rules;
using DietApi.Domain.ValueObjects;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// Each rule at its threshold and either side of it.
/// </summary>
/// <remarks>
/// A threshold that fires one unit early is how an analytics feature starts saying things that are
/// not quite true, so each is pinned rather than assumed.
/// </remarks>
public class ObservationRuleTests
{
    private static readonly DateOnly Anchor = AnalyticsFiguresBuilder.Anchor;

    // --- Late eating -------------------------------------------------------

    [Fact]
    public void Late_eating_fires_at_its_threshold_and_carries_the_share()
    {
        // 2,500 of 10,000 kcal logged at 21:00 or later is exactly 25%.
        var figures = Ordinary()
            .LoggedAt(21, 0, 2500)
            .LoggedAt(12, 0, 7500)
            .Build();

        var observation = new LateEatingRule().Evaluate(figures);

        observation.ShouldNotBeNull();
        observation.Family.ShouldBe(ObservationFamily.Timing);
        observation.Figure.ShouldBe("25%");
        observation.Text.ShouldContain("21:00");
    }

    [Fact]
    public void Late_eating_stays_quiet_just_below_its_threshold()
    {
        var figures = Ordinary()
            .LoggedAt(21, 0, 2400)
            .LoggedAt(12, 0, 7600)
            .Build();

        new LateEatingRule().Evaluate(figures).ShouldBeNull();
    }

    [Fact]
    public void Late_eating_uses_the_members_local_hours_not_UTC()
    {
        // 16:00 UTC is 21:30 at +05:30 - late for this member, not for one in London.
        var atOffset = Ordinary().LoggedAt(16, 0, 5000).LoggedAt(6, 0, 5000).InTimeZone(330).Build();
        var atUtc = Ordinary().LoggedAt(16, 0, 5000).LoggedAt(6, 0, 5000).Build();

        new LateEatingRule().Evaluate(atOffset).ShouldNotBeNull();
        new LateEatingRule().Evaluate(atUtc).ShouldBeNull();
    }

    // --- Weekend heavier ---------------------------------------------------

    [Fact]
    public void Weekend_heavier_fires_when_the_gap_clears_its_threshold()
    {
        var observation = new WeekendHeavierRule().Evaluate(WithWeekdayPattern(weekend: 2600, weekday: 2000));

        observation.ShouldNotBeNull();
        observation.Figure.ShouldContain("kcal");
        observation.Text.ShouldContain("Saturdays");
    }

    [Fact]
    public void Weekend_heavier_stays_quiet_when_the_gap_is_small()
    {
        new WeekendHeavierRule().Evaluate(WithWeekdayPattern(weekend: 2200, weekday: 2000)).ShouldBeNull();
    }

    [Fact]
    public void Weekend_heavier_needs_enough_of_both_kinds_of_day()
    {
        // One Saturday against one Tuesday is two days wearing the language of a pattern.
        var builder = Ordinary();
        builder.Day(FirstWeekday(DayOfWeek.Saturday), 4000);
        builder.Day(FirstWeekday(DayOfWeek.Tuesday), 2000);

        new WeekendHeavierRule().Evaluate(builder.Build()).ShouldBeNull();
    }

    // --- Composition -------------------------------------------------------

    [Fact]
    public void Single_food_dominance_fires_at_its_threshold()
    {
        var figures = Ordinary()
            .Meal(MealType.Dinner, 10000)
            .Food("Porridge oats", 1500, times: 20)
            .Build();

        var observation = new SingleFoodDominanceRule().Evaluate(figures);

        observation.ShouldNotBeNull();
        observation.Text.ShouldContain("Porridge oats");
        observation.Figure.ShouldBe("15%");
    }

    [Fact]
    public void Single_food_dominance_stays_quiet_below_its_threshold()
    {
        var figures = Ordinary()
            .Meal(MealType.Dinner, 10000)
            .Food("Porridge oats", 1400, times: 20)
            .Build();

        new SingleFoodDominanceRule().Evaluate(figures).ShouldBeNull();
    }

    [Fact]
    public void Meal_skew_fires_when_one_meal_dominates()
    {
        var figures = Ordinary()
            .Meal(MealType.Breakfast, 1000).Meal(MealType.Lunch, 1000)
            .Meal(MealType.Dinner, 4500).Meal(MealType.Snack, 3500)
            .Build();

        var observation = new MealSkewRule().Evaluate(figures);

        observation.ShouldNotBeNull();
        observation.Text.ShouldContain("Dinner");
    }

    [Fact]
    public void Meal_skew_stays_quiet_on_an_even_spread()
    {
        var figures = Ordinary()
            .Meal(MealType.Breakfast, 2500).Meal(MealType.Lunch, 2500)
            .Meal(MealType.Dinner, 2500).Meal(MealType.Snack, 2500)
            .Build();

        new MealSkewRule().Evaluate(figures).ShouldBeNull();
    }

    [Fact]
    public void Low_plant_share_fires_below_its_threshold_and_names_energy()
    {
        var figures = Ordinary()
            .Meal(MealType.Dinner, 10000)
            .Category(FoodCategory.Staple, 9500)
            .Category(FoodCategory.Fruit, 500)
            .Build();

        var observation = new LowPlantShareRule().Evaluate(figures);

        observation.ShouldNotBeNull();

        // Says "energy", because fruit and vegetables are low in energy by nature and a member
        // eating plenty of them still sees a small percentage.
        observation.Text.ShouldContain("energy");
    }

    [Fact]
    public void Low_plant_share_stays_quiet_when_plants_clear_the_threshold()
    {
        var figures = Ordinary()
            .Meal(MealType.Dinner, 10000)
            .Category(FoodCategory.Staple, 8500)
            .Category(FoodCategory.Fruit, 800)
            .Category(FoodCategory.Vegetable, 700)
            .Build();

        new LowPlantShareRule().Evaluate(figures).ShouldBeNull();
    }

    // --- Targets and consistency -------------------------------------------

    [Fact]
    public void Protein_below_target_fires_at_four_fifths_of_target()
    {
        var figures = OrdinaryDaysWith(protein: 120m, targetProtein: 150m);

        var observation = new ProteinBelowTargetRule().Evaluate(figures);

        observation.ShouldNotBeNull();
        observation.Family.ShouldBe(ObservationFamily.Targets);
        observation.Text.ShouldContain("150");
    }

    [Fact]
    public void Protein_below_target_stays_quiet_just_above_it()
    {
        new ProteinBelowTargetRule().Evaluate(OrdinaryDaysWith(protein: 125m, targetProtein: 150m))
            .ShouldBeNull();
    }

    [Fact]
    public void Protein_below_target_says_nothing_when_no_target_was_set()
    {
        // There is no default protein target to fall back on: a target nobody chose is not one.
        new ProteinBelowTargetRule().Evaluate(OrdinaryDaysWith(protein: 40m, targetProtein: null))
            .ShouldBeNull();
    }

    [Fact]
    public void Logging_improved_fires_on_a_clear_increase_and_stays_quiet_otherwise()
    {
        var up = Ordinary().LoggedDays(24).PreviouslyLoggedDays(16).Build();
        var flat = Ordinary().LoggedDays(24).PreviouslyLoggedDays(22).Build();
        var down = Ordinary().LoggedDays(16).PreviouslyLoggedDays(24).Build();

        var observation = new LoggingImprovedRule().Evaluate(up);
        observation.ShouldNotBeNull();
        observation.Text.ShouldContain("24");
        observation.Text.ShouldContain("16");

        new LoggingImprovedRule().Evaluate(flat).ShouldBeNull();
        new LoggingImprovedRule().Evaluate(down).ShouldBeNull();
    }

    [Fact]
    public void Logging_improved_says_nothing_without_a_comparison_window()
    {
        var wholePlan = Ordinary().ForPreset(PeriodPreset.Plan).LoggedDays(24).PreviouslyLoggedDays(4).Build();

        wholePlan.Period.HasComparison.ShouldBeFalse();
        new LoggingImprovedRule().Evaluate(wholePlan).ShouldBeNull();
    }

    // --- Helpers ----------------------------------------------------------

    private static AnalyticsFiguresBuilder Ordinary() =>
        AnalyticsFiguresBuilder.Figures().WithOrdinaryDays(24).PreviouslyLoggedDays(24);

    private static AnalyticsFigures OrdinaryDaysWith(decimal protein, decimal? targetProtein)
    {
        var builder = AnalyticsFiguresBuilder.Figures().LoggedDays(24).PreviouslyLoggedDays(24);

        for (var i = 0; i < 24; i++)
        {
            builder.Day(Anchor.AddDays(-i), 2000, protein: protein, targetProtein: targetProtein);
        }

        return builder.Build();
    }

    /// <summary>Four weeks of days, weekends at one figure and weekdays at another.</summary>
    private static AnalyticsFigures WithWeekdayPattern(int weekend, int weekday)
    {
        var builder = AnalyticsFiguresBuilder.Figures().LoggedDays(28).PreviouslyLoggedDays(28);

        for (var i = 0; i < 28; i++)
        {
            var date = Anchor.AddDays(-i);
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            builder.Day(date, isWeekend ? weekend : weekday, protein: 157.5m);
        }

        return builder.Build();
    }

    private static DateOnly FirstWeekday(DayOfWeek day)
    {
        var date = Anchor;
        while (date.DayOfWeek != day)
        {
            date = date.AddDays(-1);
        }
        return date;
    }
}
