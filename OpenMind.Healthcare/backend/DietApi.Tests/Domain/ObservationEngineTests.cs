using DietApi.Domain.Observations;
using DietApi.Domain.Observations.Rules;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// The guarantees that make written observations safe to ship.
/// </summary>
/// <remarks>
/// A feature that says confident things about a member's own data is only trustworthy if it stays
/// quiet when it does not have the evidence, and says the same thing twice when asked twice. Those
/// two properties are asserted here <em>generically</em> — over every rule that exists, and every
/// rule anyone adds later, without the test needing to know what any of them says.
/// </remarks>
public class ObservationEngineTests
{
    /// <summary>Every rule the application registers. A new rule added here is covered by all of these.</summary>
    public static IReadOnlyList<IObservationRule> AllRules() =>
    [
        new LateEatingRule(),
        new WeekendHeavierRule(),
        new SingleFoodDominanceRule(),
        new MealSkewRule(),
        new LowPlantShareRule(),
        new ProteinBelowTargetRule(),
        new LoggingImprovedRule()
    ];

    private static ObservationEngine Engine() => new(AllRules());

    public static TheoryData<string> RuleNames()
    {
        var data = new TheoryData<string>();
        foreach (var rule in AllRules())
        {
            data.Add(rule.GetType().Name);
        }
        return data;
    }

    // --- The minimum, asserted over every rule -----------------------------

    [Theory]
    [MemberData(nameof(RuleNames))]
    public void No_rule_speaks_below_its_own_minimum(string ruleName)
    {
        var rule = AllRules().Single(r => r.GetType().Name == ruleName);

        // Figures deliberately extreme enough to trip every threshold in the application, so the
        // only thing that can hold a rule back is the day count.
        var figures = Extreme(loggedDays: rule.MinimumLoggedDays - 1);

        new ObservationEngine([rule]).Observe(figures).ShouldBeEmpty(
            $"{ruleName} fired on {figures.Period.LoggedDays} logged days, "
            + $"below its stated minimum of {rule.MinimumLoggedDays}");
    }

    [Fact]
    public void The_engine_reports_the_fewest_days_any_rule_needs()
    {
        Engine().MinimumDaysForAnyObservation.ShouldBe(ObservationThresholds.MinimumLoggedDays);
    }

    [Fact]
    public void A_member_with_nine_logged_days_is_told_nothing_at_all()
    {
        Engine().Observe(Extreme(loggedDays: 9)).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(RuleNames))]
    public void Every_rule_declares_a_minimum_of_at_least_a_fortnight(string ruleName)
    {
        var rule = AllRules().Single(r => r.GetType().Name == ruleName);

        rule.MinimumLoggedDays.ShouldBeGreaterThanOrEqualTo(ObservationThresholds.MinimumLoggedDays);
        rule.ThresholdDescription.ShouldNotBeNullOrWhiteSpace();
    }

    // --- Determinism -------------------------------------------------------

    [Fact]
    public void The_same_figures_produce_the_same_list_in_the_same_order()
    {
        var figures = Extreme(loggedDays: 24);
        var engine = Engine();

        var first = engine.Observe(figures);
        var second = engine.Observe(figures);

        first.Count.ShouldBe(second.Count);
        first.Select(o => o.Text).ShouldBe(second.Select(o => o.Text));
        first.Select(o => o.Strength).ShouldBe(second.Select(o => o.Strength));
    }

    [Fact]
    public void The_order_does_not_depend_on_the_order_the_rules_were_registered()
    {
        var figures = Extreme(loggedDays: 24);

        var forwards = new ObservationEngine(AllRules()).Observe(figures);
        var backwards = new ObservationEngine(AllRules().Reverse()).Observe(figures);

        forwards.Select(o => o.Text).ShouldBe(backwards.Select(o => o.Text));
    }

    // --- De-duplication ----------------------------------------------------

    [Fact]
    public void Only_the_strongest_observation_of_a_family_survives()
    {
        // Both timing rules trip on these figures; a member should see one timing observation.
        var figures = Extreme(loggedDays: 24);

        var observations = Engine().Observe(figures);

        observations.Select(o => o.Family).Distinct().Count().ShouldBe(observations.Count);
    }

    [Fact]
    public void The_survivor_of_a_family_is_the_stronger_of_the_two()
    {
        var figures = Extreme(loggedDays: 24);

        var bothTiming = new ObservationEngine([new LateEatingRule(), new WeekendHeavierRule()]);
        var separately = new[]
        {
            new ObservationEngine([new LateEatingRule()]).Observe(figures).SingleOrDefault(),
            new ObservationEngine([new WeekendHeavierRule()]).Observe(figures).SingleOrDefault()
        }.Where(o => o is not null).ToList();

        var combined = bothTiming.Observe(figures);

        combined.Count.ShouldBe(1);
        combined[0].Strength.ShouldBe(separately.Max(o => o!.Strength));
    }

    // --- Silence as an answer ----------------------------------------------

    [Fact]
    public void Nothing_fires_on_an_unremarkable_period()
    {
        // Plenty of days, and nothing about them past any threshold.
        var figures = AnalyticsFiguresBuilder.Figures()
            .WithOrdinaryDays(24)
            .Meal(MealType.Breakfast, 5000).Meal(MealType.Lunch, 5000)
            .Meal(MealType.Dinner, 5000).Meal(MealType.Snack, 5000)
            .Category(FoodCategory.Fruit, 4000).Category(FoodCategory.Vegetable, 4000)
            .Category(FoodCategory.Staple, 6000).Category(FoodCategory.Protein, 6000)
            .Food("Porridge oats", 1000).Food("Banana", 900)
            .LoggedAt(8, 0, 7000).LoggedAt(13, 0, 7000).LoggedAt(18, 0, 6000)
            .PreviouslyLoggedDays(24)
            .Build();

        Engine().Observe(figures).ShouldBeEmpty();
    }

    [Fact]
    public void An_engine_with_no_rules_is_silent_rather_than_broken()
    {
        var engine = new ObservationEngine([]);

        engine.Observe(Extreme(loggedDays: 30)).ShouldBeEmpty();
        engine.MinimumDaysForAnyObservation.ShouldBe(0);
    }

    // --- Every observation carries its evidence ----------------------------

    [Fact]
    public void Every_observation_carries_a_figure_and_the_days_behind_it()
    {
        var observations = Engine().Observe(Extreme(loggedDays: 24));

        observations.ShouldNotBeEmpty();
        observations.ShouldAllBe(o => !string.IsNullOrWhiteSpace(o.Figure));
        observations.ShouldAllBe(o => o.BasedOnDays > 0);
        observations.ShouldAllBe(o => o.Strength >= 0m && o.Strength <= 1m);
    }

    /// <summary>
    /// Figures extreme enough to trip every threshold the application has: late evening eating,
    /// heavy weekends, one dominant food, a skewed meal, almost no plants, protein well under
    /// target, and a big jump in logged days.
    /// </summary>
    private static AnalyticsFigures Extreme(int loggedDays)
    {
        var builder = AnalyticsFiguresBuilder.Figures()
            .LoggedDays(loggedDays)
            .PreviouslyLoggedDays(Math.Max(1, loggedDays / 2))
            .Meal(MealType.Breakfast, 1000)
            .Meal(MealType.Lunch, 2000)
            .Meal(MealType.Dinner, 12000)
            .Meal(MealType.Snack, 1000)
            .Category(FoodCategory.Staple, 15000)
            .Category(FoodCategory.Fruit, 100)
            .Food("Catering tub of oil", 6000, times: 20)
            .Food("Banana", 500, times: 5)
            .LoggedAt(22, 0, 9000)
            .LoggedAt(12, 0, 7000);

        // Weekends deliberately heavy, weekdays deliberately light, protein deliberately short.
        var anchor = AnalyticsFiguresBuilder.Anchor;
        for (var i = 0; i < Math.Max(loggedDays, 1); i++)
        {
            var date = anchor.AddDays(-i);
            var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            builder.Day(date, weekend ? 3200 : 2000, protein: 60m, targetProtein: 150m);
        }

        return builder.Build();
    }
}
