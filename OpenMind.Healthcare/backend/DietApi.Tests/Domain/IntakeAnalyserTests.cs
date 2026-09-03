using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.Domain;

/// <summary>
/// The reconciliation invariants, asserted directly.
/// </summary>
/// <remarks>
/// A breakdown whose parts do not sum to its total is the one bug in a reporting feature a member
/// will always find, because adding four numbers is exactly what someone does when a figure
/// surprises them. SC-002 is the criterion; these are its teeth.
/// </remarks>
public class IntakeAnalyserTests
{
    private readonly IntakeAnalyser _analyser = new();

    private static readonly DateOnly Day = new(2026, 3, 1);

    [Fact]
    public void Meal_energies_sum_to_the_period_total()
    {
        var breakdown = _analyser.BreakDownByMeal(
        [
            new MealIntakeRow(MealType.Breakfast, 10368, 41),
            new MealIntakeRow(MealType.Lunch, 14515, 52),
            new MealIntakeRow(MealType.Dinner, 20736, 48),
            new MealIntakeRow(MealType.Snack, 6221, 63)
        ]);

        breakdown.TotalKilocalories.ShouldBe(51840);
        breakdown.Shares.Sum(s => s.Kilocalories).ShouldBe(51840);
    }

    [Fact]
    public void Meal_shares_sum_to_exactly_one_hundred_after_rounding()
    {
        // Thirds do not divide into tenths of a percent, so this is the case naive rounding fails.
        var breakdown = _analyser.BreakDownByMeal(
        [
            new MealIntakeRow(MealType.Breakfast, 1000, 1),
            new MealIntakeRow(MealType.Lunch, 1000, 1),
            new MealIntakeRow(MealType.Dinner, 1000, 1),
            new MealIntakeRow(MealType.Snack, 0, 0)
        ]);

        breakdown.Shares.Sum(s => s.ShareOfTotal).ShouldBe(100m);
    }

    [Fact]
    public void Every_meal_appears_even_when_nothing_was_logged_for_it()
    {
        var breakdown = _analyser.BreakDownByMeal([new MealIntakeRow(MealType.Dinner, 2000, 4)]);

        breakdown.Shares.Count.ShouldBe(Enum.GetValues<MealType>().Length);
        breakdown.Shares.Single(s => s.Meal == MealType.Breakfast).Kilocalories.ShouldBe(0);
        breakdown.Shares.Single(s => s.Meal == MealType.Dinner).ShareOfTotal.ShouldBe(100m);
    }

    [Fact]
    public void Category_energies_sum_to_the_same_total_as_the_meals()
    {
        var meals = _analyser.BreakDownByMeal(
        [
            new MealIntakeRow(MealType.Breakfast, 500, 2),
            new MealIntakeRow(MealType.Dinner, 1500, 3)
        ]);

        var categories = _analyser.BreakDownByCategory(
        [
            new CategoryIntakeRow(FoodCategory.Staple, 1200),
            new CategoryIntakeRow(FoodCategory.Protein, 800)
        ]);

        categories.TotalKilocalories.ShouldBe(meals.TotalKilocalories);
        categories.Shares.Sum(s => s.ShareOfTotal).ShouldBe(100m);
    }

    [Fact]
    public void A_period_with_nothing_logged_gives_zero_shares_rather_than_dividing_by_zero()
    {
        var meals = _analyser.BreakDownByMeal([]);
        var categories = _analyser.BreakDownByCategory([]);

        meals.TotalKilocalories.ShouldBe(0);
        meals.Shares.ShouldAllBe(s => s.ShareOfTotal == 0m);
        categories.Shares.ShouldAllBe(s => s.ShareOfTotal == 0m);
    }

    [Fact]
    public void Top_food_shares_are_of_the_whole_period_not_of_the_top_ten()
    {
        // "18% of everything you logged" must mean that, so these deliberately do not sum to 100.
        var foods = _analyser.TopFoods(
        [
            new FoodContributionRow(Guid.NewGuid(), "Porridge oats", 1800, 24),
            new FoodContributionRow(Guid.NewGuid(), "Banana", 1200, 12)
        ], periodTotalKilocalories: 10000);

        foods[0].ShareOfTotal.ShouldBe(18.0m);
        foods[1].ShareOfTotal.ShouldBe(12.0m);
        foods.Sum(f => f.ShareOfTotal).ShouldBeLessThan(100m);
    }

    [Fact]
    public void The_plant_share_adds_fruit_and_vegetables_together()
    {
        var categories = _analyser.BreakDownByCategory(
        [
            new CategoryIntakeRow(FoodCategory.Fruit, 600),
            new CategoryIntakeRow(FoodCategory.Vegetable, 400),
            new CategoryIntakeRow(FoodCategory.Staple, 9000)
        ]);

        categories.PlantShare.ShouldBe(10.0m);
    }

    [Fact]
    public void A_days_state_matches_the_assessment_the_rest_of_the_programme_uses()
    {
        var summary = _analyser.Summarise(
        [
            Row(Day, calories: 1800, target: 2100),
            Row(Day.AddDays(1), calories: 2500, target: 2100),
            Row(Day.AddDays(2), calories: 2100, target: 2100)
        ], totalDays: 3);

        summary.OnTargetDays.ShouldBe(2);
        summary.OverTargetDays.ShouldBe(1);
        summary.NotLoggedDays.ShouldBe(0);
    }

    [Fact]
    public void Each_day_is_judged_against_its_own_stored_target()
    {
        // The same intake, on either side of a target change, lands on either side of the line.
        var summary = _analyser.Summarise(
        [
            Row(Day, calories: 2200, target: 2400),
            Row(Day.AddDays(1), calories: 2200, target: 2000)
        ], totalDays: 2);

        summary.OnTargetDays.ShouldBe(1);
        summary.OverTargetDays.ShouldBe(1);
    }

    [Fact]
    public void The_previous_window_average_is_null_when_there_is_no_comparison()
    {
        _analyser.Summarise([Row(Day, 2000, 2100)], totalDays: 1)
            .PreviousAverageDailyKilocalories.ShouldBeNull();

        _analyser.Summarise([Row(Day, 2000, 2100)], totalDays: 1, previousDays: [])
            .PreviousAverageDailyKilocalories.ShouldBeNull();

        _analyser.Summarise([Row(Day, 2000, 2100)], totalDays: 1, previousDays: [Row(Day.AddDays(-1), 1800, 2100)])
            .PreviousAverageDailyKilocalories.ShouldBe(1800);
    }

    internal static DayIntakeRow Row(
        DateOnly date, int calories, int target,
        decimal protein = 100m, decimal carbs = 200m, decimal fat = 70m,
        decimal? targetProtein = 157.5m, decimal? targetCarbs = 210m, decimal? targetFat = 70m) =>
        new(date, calories, protein, carbs, fat, target, targetProtein, targetCarbs, targetFat);
}
