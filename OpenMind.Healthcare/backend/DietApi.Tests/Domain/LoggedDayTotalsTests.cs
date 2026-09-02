using DietApi.Domain.Aggregates;
using DietApi.Domain.ValueObjects;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// The stored daily total duplicates what the entries already say. That duplication is only safe
/// because the aggregate recomputes it on every mutation - so this asserts the invariant
/// directly, and asserts that the concurrency token moves with it.
/// </summary>
public class LoggedDayTotalsTests
{
    [Fact]
    public void A_new_day_starts_at_zero()
    {
        var day = LoggedDayBuilder.ADay().Build();

        day.Totals.Calories.ShouldBe(0);
        day.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Adding_an_entry_keeps_the_total_equal_to_the_sum_of_entries()
    {
        var day = LoggedDayBuilder.ADay().Build();
        var oats = FakeFoodLibraryRepository.Oats();
        var banana = FakeFoodLibraryRepository.Banana();

        AddOne(day, oats, MealType.Breakfast);
        AssertInvariant(day);
        day.Totals.Calories.ShouldBe(228);

        AddOne(day, banana, MealType.Snack);
        AssertInvariant(day);
        day.Totals.Calories.ShouldBe(333);
    }

    [Fact]
    public void Updating_an_entry_keeps_the_total_equal_to_the_sum_of_entries()
    {
        var day = LoggedDayBuilder.ADay().Ate(FakeFoodLibraryRepository.Oats()).Build();
        var oats = FakeFoodLibraryRepository.Oats();
        var hundredGrams = oats.ServingSizes.ElementAt(1);
        var entryId = day.Entries.Single().Id;

        day.UpdateEntry(entryId, hundredGrams.Id, hundredGrams.Label, 2m, MealType.Breakfast, hundredGrams.Nutrition);

        AssertInvariant(day);
        day.Totals.Calories.ShouldBe(760);
    }

    [Fact]
    public void Removing_an_entry_keeps_the_total_equal_to_the_sum_of_entries()
    {
        var day = LoggedDayBuilder.ADay()
            .Ate(FakeFoodLibraryRepository.Oats())
            .Ate(FakeFoodLibraryRepository.Banana(), meal: MealType.Snack)
            .Build();

        day.RemoveEntry(day.Entries.First(e => e.MealType == MealType.Snack).Id).ShouldBeTrue();

        AssertInvariant(day);
        day.Totals.Calories.ShouldBe(228);
    }

    [Fact]
    public void Removing_an_entry_that_is_not_there_changes_nothing()
    {
        var day = LoggedDayBuilder.ADay().Ate(FakeFoodLibraryRepository.Oats()).Build();
        var before = day.Totals.Calories;

        day.RemoveEntry(Guid.NewGuid()).ShouldBeFalse();

        day.Totals.Calories.ShouldBe(before);
    }

    [Fact]
    public void Macronutrient_totals_track_the_entries_too()
    {
        var day = LoggedDayBuilder.ADay()
            .Ate(FakeFoodLibraryRepository.Oats())
            .Ate(FakeFoodLibraryRepository.Banana(), meal: MealType.Snack)
            .Build();

        day.Totals.ProteinG.ShouldBe(9.7m);
        day.Totals.CarbsG.ShouldBe(63.0m);
        day.Totals.FatG.ShouldBe(5.2m);
    }

    [Fact]
    public void Every_mutation_reassigns_the_concurrency_token()
    {
        // Without this, two devices editing the same day could each save a total that disagrees
        // with the entries the other one wrote.
        var day = LoggedDayBuilder.ADay().Build();
        var afterStart = day.Version;

        AddOne(day, FakeFoodLibraryRepository.Oats(), MealType.Breakfast);
        var afterAdd = day.Version;
        afterAdd.ShouldNotBe(afterStart);

        var entryId = day.Entries.Single().Id;
        var oats = FakeFoodLibraryRepository.Oats();
        var serving = oats.ServingSizes.First();
        day.UpdateEntry(entryId, serving.Id, serving.Label, 2m, MealType.Breakfast, serving.Nutrition);
        var afterUpdate = day.Version;
        afterUpdate.ShouldNotBe(afterAdd);

        day.RemoveEntry(entryId);
        day.Version.ShouldNotBe(afterUpdate);
    }

    [Fact]
    public void Entries_are_grouped_by_meal_for_display()
    {
        var day = LoggedDayBuilder.ADay()
            .Ate(FakeFoodLibraryRepository.Oats(), meal: MealType.Breakfast)
            .Ate(FakeFoodLibraryRepository.Banana(), meal: MealType.Snack)
            .Build();

        var byMeal = day.EntriesByMeal();

        byMeal[MealType.Breakfast].Count.ShouldBe(1);
        byMeal[MealType.Snack].Count.ShouldBe(1);
        byMeal.ContainsKey(MealType.Dinner).ShouldBeFalse();
    }

    private static void AddOne(LoggedDay day, FoodLibraryItem food, MealType meal)
    {
        var serving = food.ServingSizes.First();
        day.AddEntry(food.Id, serving.Id, food.Name, serving.Label, 1m, meal, serving.Nutrition);
    }

    /// <summary>The whole reason the stored total is safe to trust.</summary>
    private static void AssertInvariant(LoggedDay day) =>
        day.Totals.Calories.ShouldBe(day.Entries.Sum(e => e.Nutrition.Calories));
}
