using DDD.BuildingBlocks;
using DietApi.Domain.Aggregates;
using DietApi.Domain.Rules;
using DietApi.Domain.ValueObjects;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// The rules guarding a food entry. Each one exists because breaking it silently distorts every
/// statistic that follows.
/// </summary>
public class FoodEntryRulesTests
{
    [Fact]
    public void A_day_cannot_be_started_in_the_future()
    {
        var builder = LoggedDayBuilder.ADay();

        var act = () =>
        {
            LoggedDay.StartDay(
                builder.PlanId, builder.UserId, builder.Today.AddDays(1),
                builder.Targets, builder.PlanStartDate, builder.Clock);
        };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(EntryDateCannotBeInFutureRule));
    }

    [Fact]
    public void A_day_cannot_predate_the_plan_it_belongs_to()
    {
        var builder = LoggedDayBuilder.ADay().PlanStartedDaysAgo(10);

        var act = () =>
        {
            LoggedDay.StartDay(
                builder.PlanId, builder.UserId, builder.Today.AddDays(-11),
                builder.Targets, builder.PlanStartDate, builder.Clock);
        };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(EntryDateCannotPrecedePlanStartRule));
    }

    [Fact]
    public void A_day_on_the_plan_start_date_is_allowed()
    {
        var builder = LoggedDayBuilder.ADay().PlanStartedDaysAgo(10).DaysAgo(10);

        Should.NotThrow(() => builder.Build());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_quantity_must_be_positive(decimal quantity)
    {
        var day = LoggedDayBuilder.ADay().Build();
        var food = FakeFoodLibraryRepository.Oats();
        var serving = food.ServingSizes.First();

        var act = () =>
        {
            day.AddEntry(food.Id, serving.Id, food.Name, serving.Label,
                quantity, MealType.Breakfast, serving.Nutrition);
        };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(QuantityMustBePositiveRule));
    }

    [Fact]
    public void A_fractional_quantity_is_allowed()
    {
        var day = LoggedDayBuilder.ADay()
            .Ate(FakeFoodLibraryRepository.Oats(), quantity: 0.5m)
            .Build();

        // 228 kcal halved, rounded away from zero.
        day.Totals.Calories.ShouldBe(114);
    }

    [Fact]
    public void A_single_entry_cannot_exceed_the_calorie_ceiling()
    {
        var day = LoggedDayBuilder.ADay().Build();
        var food = FakeFoodLibraryRepository.Enormous();
        var serving = food.ServingSizes.First();

        // 9,500 kcal a tub, twice over, is past the 10,000 ceiling.
        var act = () =>
        {
            day.AddEntry(food.Id, serving.Id, food.Name, serving.Label,
                2m, MealType.Dinner, serving.Nutrition);
        };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(EntryCaloriesWithinCeilingRule));
    }

    [Fact]
    public void An_entry_at_the_ceiling_is_allowed()
    {
        var day = LoggedDayBuilder.ADay().Build();
        var food = FakeFoodLibraryRepository.Enormous();
        var serving = food.ServingSizes.First();

        Should.NotThrow(() => day.AddEntry(
            food.Id, serving.Id, food.Name, serving.Label,
            1m, MealType.Dinner, serving.Nutrition));
    }

    [Fact]
    public void Updating_an_entry_that_is_not_on_this_day_is_refused()
    {
        var day = LoggedDayBuilder.ADay().Ate(FakeFoodLibraryRepository.Oats()).Build();
        var food = FakeFoodLibraryRepository.Oats();
        var serving = food.ServingSizes.First();

        var act = () =>
        {
            day.UpdateEntry(Guid.NewGuid(), serving.Id, serving.Label, 1m, MealType.Lunch, serving.Nutrition);
        };

        act.ShouldThrow<DomainException>();
    }
}
