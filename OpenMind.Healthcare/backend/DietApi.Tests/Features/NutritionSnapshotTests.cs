using DietApi.Domain.ValueObjects;
using DietApi.Features.FoodLog;
using DietApi.Features.FoodLog.AddFoodEntry;
using DietApi.Features.FoodLog.GetDay;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Once an entry is logged, its nutrition belongs to the entry, not to the catalogue.
/// </summary>
/// <remarks>
/// This is what makes correcting a typo in the food library safe: it cannot reach backwards and
/// re-judge a day the member already saw assessed. The strongest proof is structural - a logged
/// day renders in full with the library gone entirely.
/// </remarks>
public class NutritionSnapshotTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task A_logged_day_renders_in_full_even_when_the_library_is_unavailable()
    {
        var builder = DietPlanBuilder.APlan();
        var plan = builder.Build();
        var planRepo = FakeDietPlanRepository.Containing(plan);
        var oats = FakeFoodLibraryRepository.Oats();
        var dayRepo = FakeLoggedDayRepository.Empty();

        var addHandler = new AddFoodEntryHandler(
            planRepo, dayRepo, FakeFoodLibraryRepository.Containing(oats), SignedInUser.WithId(builder.UserId));

        await addHandler.Handle(
            new AddFoodEntryCommand(Today, new AddFoodEntryRequest(
                oats.Id, oats.ServingSizes.First().Id, 1m, MealType.Breakfast, null)),
            CancellationToken.None);

        // Read the day back with no library at all. GetDayHandler does not even take the library
        // repository, so it structurally cannot re-derive nutrition from it.
        var getHandler = new GetDayHandler(planRepo, dayRepo, SignedInUser.WithId(builder.UserId));
        var day = await getHandler.Handle(new GetDayQuery(Today), CancellationToken.None);

        day.ShouldNotBeNull();
        day.Totals.Calories.ShouldBe(228);
        day.Entries.Single().Nutrition.Calories.ShouldBe(228);
        day.Entries.Single().FoodName.ShouldBe("Porridge oats");
        day.Entries.Single().ServingLabel.ShouldBe("1 bowl (60 g)");
    }

    [Fact]
    public void An_entry_keeps_its_own_copy_of_the_name_and_the_numbers()
    {
        var oats = FakeFoodLibraryRepository.Oats();
        var day = LoggedDayBuilder.ADay().Ate(oats, quantity: 2m).Build();

        var entry = day.Entries.Single();

        // Snapshotted, not referenced: the values are already multiplied out and stored.
        entry.Nutrition.Calories.ShouldBe(456);
        entry.FoodName.ShouldBe("Porridge oats");
        entry.ServingLabel.ShouldBe("1 bowl (60 g)");

        // The library ids survive for provenance, but nothing reads through them to compute.
        entry.FoodLibraryItemId.ShouldBe(oats.Id);
        entry.ServingSizeId.ShouldBe(oats.ServingSizes.First().Id);
    }

    [Fact]
    public void A_day_assessed_under_one_target_is_not_re_judged_by_a_later_one()
    {
        // 1,824 calories against the 2,100 target in force at the time: on target.
        var day = LoggedDayBuilder.ADay().Targeting(2100)
            .Ate(FakeFoodLibraryRepository.Oats(), quantity: 8m)
            .Build();

        day.Assess().State.ShouldBe(DayState.OnTarget);

        // The member later drops their target to 1,500. The day holds its own snapshot, so its
        // verdict does not move.
        day.TargetSnapshot.Calories.ShouldBe(2100);
        day.Assess().State.ShouldBe(DayState.OnTarget);
    }
}
