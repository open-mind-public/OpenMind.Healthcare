using DDD.BuildingBlocks;
using DietPlanAggregate = DietApi.Domain.Aggregates.DietPlan;
using FoodItem = DietApi.Domain.Aggregates.FoodLibraryItem;
using DietApi.Domain;
using DietApi.Domain.ValueObjects;
using DietApi.Features.FoodLog;
using DietApi.Features.FoodLog.AddFoodEntry;
using DietApi.Features.FoodLog.DeleteFoodEntry;
using DietApi.Features.FoodLog.GetDay;
using DietApi.Features.FoodLog.UpdateFoodEntry;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for the daily logging use cases.
/// </summary>
public class FoodLogHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // --- Get day ----------------------------------------------------------

    [Fact]
    public async Task An_unlogged_date_returns_a_not_logged_day_rather_than_a_404()
    {
        var (plan, planRepo, userId) = APlan();
        var handler = new GetDayHandler(planRepo, FakeLoggedDayRepository.Empty(), SignedInUser.WithId(userId));

        var day = await handler.Handle(new GetDayQuery(Today), CancellationToken.None);

        day.ShouldNotBeNull();
        day.State.ShouldBe(DayState.NotLogged);
        day.Totals.Calories.ShouldBe(0);
        day.RemainingCalories.ShouldBe(plan.Targets.Calories);
        day.Version.ShouldBeNull();
        day.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = new GetDayHandler(
            FakeDietPlanRepository.Empty(), FakeLoggedDayRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        (await handler.Handle(new GetDayQuery(Today), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task A_future_date_is_refused()
    {
        var (_, planRepo, userId) = APlan();
        var handler = new GetDayHandler(planRepo, FakeLoggedDayRepository.Empty(), SignedInUser.WithId(userId));

        await Should.ThrowAsync<DomainException>(
            handler.Handle(new GetDayQuery(Today.AddDays(1)), CancellationToken.None));
    }

    [Fact]
    public async Task A_date_before_the_plan_started_is_refused()
    {
        var (_, planRepo, userId) = APlan(startedDaysAgo: 10);
        var handler = new GetDayHandler(planRepo, FakeLoggedDayRepository.Empty(), SignedInUser.WithId(userId));

        await Should.ThrowAsync<DomainException>(
            handler.Handle(new GetDayQuery(Today.AddDays(-11)), CancellationToken.None));
    }

    [Fact]
    public async Task Fetching_a_day_without_a_signed_in_member_is_refused()
    {
        var handler = new GetDayHandler(
            FakeDietPlanRepository.Empty(), FakeLoggedDayRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetDayQuery(Today), CancellationToken.None));
    }

    // --- Add entry --------------------------------------------------------

    [Fact]
    public async Task Adding_the_first_entry_creates_the_day_and_returns_the_updated_totals()
    {
        var (plan, planRepo, userId) = APlan();
        var oats = FakeFoodLibraryRepository.Oats();
        var dayRepo = FakeLoggedDayRepository.Empty();
        var handler = new AddFoodEntryHandler(
            planRepo, dayRepo, FakeFoodLibraryRepository.Containing(oats), SignedInUser.WithId(userId));

        var day = await handler.Handle(
            new AddFoodEntryCommand(Today, Request(oats)), CancellationToken.None);

        dayRepo.SaveCount.ShouldBe(1);
        day.Totals.Calories.ShouldBe(228);
        day.State.ShouldBe(DayState.OnTarget);
        day.Version.ShouldNotBeNull();

        // The day snapshotted the plan's target, so a later target change cannot re-judge it.
        day.Targets.Calories.ShouldBe(plan.Targets.Calories);
    }

    [Fact]
    public async Task Adding_a_second_entry_needs_the_current_version_and_accumulates()
    {
        var (_, planRepo, userId) = APlan();
        var oats = FakeFoodLibraryRepository.Oats();
        var banana = FakeFoodLibraryRepository.Banana();
        var dayRepo = FakeLoggedDayRepository.Empty();
        var library = FakeFoodLibraryRepository.Containing(oats, banana);
        var handler = new AddFoodEntryHandler(planRepo, dayRepo, library, SignedInUser.WithId(userId));

        var first = await handler.Handle(new AddFoodEntryCommand(Today, Request(oats)), CancellationToken.None);

        var second = await handler.Handle(
            new AddFoodEntryCommand(Today, Request(banana, version: first.Version)), CancellationToken.None);

        second.Totals.Calories.ShouldBe(333);
        second.Entries.Count.ShouldBe(2);
        second.Version.ShouldNotBe(first.Version);
    }

    [Fact]
    public async Task A_stale_version_is_refused_rather_than_overwriting_the_other_session()
    {
        var (_, planRepo, userId) = APlan();
        var oats = FakeFoodLibraryRepository.Oats();
        var banana = FakeFoodLibraryRepository.Banana();
        var dayRepo = FakeLoggedDayRepository.Empty();
        var library = FakeFoodLibraryRepository.Containing(oats, banana);
        var handler = new AddFoodEntryHandler(planRepo, dayRepo, library, SignedInUser.WithId(userId));

        var first = await handler.Handle(new AddFoodEntryCommand(Today, Request(oats)), CancellationToken.None);
        var staleVersion = first.Version;

        // Another session adds something, moving the version on.
        await handler.Handle(new AddFoodEntryCommand(Today, Request(banana, version: staleVersion)), CancellationToken.None);

        // The first session tries again with the version it still remembers.
        await Should.ThrowAsync<ConcurrencyConflictException>(
            handler.Handle(new AddFoodEntryCommand(Today, Request(banana, version: staleVersion)), CancellationToken.None));

        // Nothing was lost: both earlier entries are still there.
        dayRepo.Stored.Single().Entries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Logging_before_a_plan_exists_is_refused()
    {
        var oats = FakeFoodLibraryRepository.Oats();
        var handler = new AddFoodEntryHandler(
            FakeDietPlanRepository.Empty(), FakeLoggedDayRepository.Empty(),
            FakeFoodLibraryRepository.Containing(oats), SignedInUser.WithId(Guid.NewGuid()));

        await Should.ThrowAsync<DomainException>(
            handler.Handle(new AddFoodEntryCommand(Today, Request(oats)), CancellationToken.None));
    }

    [Fact]
    public async Task Logging_a_food_that_is_not_in_the_library_is_refused()
    {
        var (_, planRepo, userId) = APlan();
        var oats = FakeFoodLibraryRepository.Oats();
        var handler = new AddFoodEntryHandler(
            planRepo, FakeLoggedDayRepository.Empty(), FakeFoodLibraryRepository.Empty(), SignedInUser.WithId(userId));

        await Should.ThrowAsync<DomainException>(
            handler.Handle(new AddFoodEntryCommand(Today, Request(oats)), CancellationToken.None));
    }

    [Fact]
    public async Task Logging_a_future_date_is_refused()
    {
        var (_, planRepo, userId) = APlan();
        var oats = FakeFoodLibraryRepository.Oats();
        var handler = new AddFoodEntryHandler(
            planRepo, FakeLoggedDayRepository.Empty(),
            FakeFoodLibraryRepository.Containing(oats), SignedInUser.WithId(userId));

        await Should.ThrowAsync<BusinessRuleValidationException>(
            handler.Handle(new AddFoodEntryCommand(Today.AddDays(1), Request(oats)), CancellationToken.None));
    }

    [Fact]
    public async Task Logging_before_the_plan_start_date_is_refused()
    {
        var (_, planRepo, userId) = APlan(startedDaysAgo: 5);
        var oats = FakeFoodLibraryRepository.Oats();
        var handler = new AddFoodEntryHandler(
            planRepo, FakeLoggedDayRepository.Empty(),
            FakeFoodLibraryRepository.Containing(oats), SignedInUser.WithId(userId));

        await Should.ThrowAsync<BusinessRuleValidationException>(
            handler.Handle(new AddFoodEntryCommand(Today.AddDays(-6), Request(oats)), CancellationToken.None));
    }

    [Fact]
    public async Task Adding_an_entry_without_a_signed_in_member_is_refused()
    {
        var oats = FakeFoodLibraryRepository.Oats();
        var handler = new AddFoodEntryHandler(
            FakeDietPlanRepository.Empty(), FakeLoggedDayRepository.Empty(),
            FakeFoodLibraryRepository.Containing(oats), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new AddFoodEntryCommand(Today, Request(oats)), CancellationToken.None));
    }

    // --- Update entry -----------------------------------------------------

    [Fact]
    public async Task Editing_an_entry_recalculates_the_day()
    {
        var oats = FakeFoodLibraryRepository.Oats();
        var day = LoggedDayBuilder.ADay().Ate(oats).Build();
        var dayRepo = FakeLoggedDayRepository.Containing(day);
        var handler = new UpdateFoodEntryHandler(
            dayRepo, FakeFoodLibraryRepository.Containing(oats), SignedInUser.WithId(day.UserId));

        var hundredGrams = oats.ServingSizes.ElementAt(1);
        var result = await handler.Handle(
            new UpdateFoodEntryCommand(day.Entries.Single().Id,
                new UpdateFoodEntryRequest(hundredGrams.Id, 2m, MealType.Lunch, day.Version)),
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.Totals.Calories.ShouldBe(760);
        result.Entries.Single().MealType.ShouldBe(MealType.Lunch);
    }

    [Fact]
    public async Task Editing_another_member_s_entry_is_not_possible()
    {
        var oats = FakeFoodLibraryRepository.Oats();
        var day = LoggedDayBuilder.ADay().Ate(oats).Build();
        var handler = new UpdateFoodEntryHandler(
            FakeLoggedDayRepository.Containing(day),
            FakeFoodLibraryRepository.Containing(oats),
            SignedInUser.WithId(Guid.NewGuid()));

        var serving = oats.ServingSizes.First();
        var result = await handler.Handle(
            new UpdateFoodEntryCommand(day.Entries.Single().Id,
                new UpdateFoodEntryRequest(serving.Id, 1m, MealType.Lunch, day.Version)),
            CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Editing_with_a_stale_version_is_refused()
    {
        var oats = FakeFoodLibraryRepository.Oats();
        var day = LoggedDayBuilder.ADay().Ate(oats).Build();
        var handler = new UpdateFoodEntryHandler(
            FakeLoggedDayRepository.Containing(day),
            FakeFoodLibraryRepository.Containing(oats),
            SignedInUser.WithId(day.UserId));

        var serving = oats.ServingSizes.First();
        await Should.ThrowAsync<ConcurrencyConflictException>(
            handler.Handle(new UpdateFoodEntryCommand(day.Entries.Single().Id,
                new UpdateFoodEntryRequest(serving.Id, 1m, MealType.Lunch, Guid.NewGuid())),
                CancellationToken.None));
    }

    [Fact]
    public async Task Editing_without_a_signed_in_member_is_refused()
    {
        var handler = new UpdateFoodEntryHandler(
            FakeLoggedDayRepository.Empty(), FakeFoodLibraryRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new UpdateFoodEntryCommand(Guid.NewGuid(),
                new UpdateFoodEntryRequest(Guid.NewGuid(), 1m, MealType.Lunch, Guid.NewGuid())),
                CancellationToken.None));
    }

    // --- Delete entry -----------------------------------------------------

    [Fact]
    public async Task Deleting_one_of_several_entries_keeps_the_day()
    {
        var day = LoggedDayBuilder.ADay()
            .Ate(FakeFoodLibraryRepository.Oats())
            .Ate(FakeFoodLibraryRepository.Banana(), meal: MealType.Snack)
            .Build();
        var dayRepo = FakeLoggedDayRepository.Containing(day);
        var handler = new DeleteFoodEntryHandler(dayRepo, SignedInUser.WithId(day.UserId));

        var result = await handler.Handle(
            new DeleteFoodEntryCommand(day.Entries.First().Id, day.Version), CancellationToken.None);

        result.Found.ShouldBeTrue();
        result.Day.ShouldNotBeNull();
        result.Day.Entries.Count.ShouldBe(1);
        dayRepo.DeleteCount.ShouldBe(0);
    }

    [Fact]
    public async Task Deleting_the_last_entry_removes_the_day_so_the_date_reverts_to_not_logged()
    {
        var day = LoggedDayBuilder.ADay().Ate(FakeFoodLibraryRepository.Oats()).Build();
        var dayRepo = FakeLoggedDayRepository.Containing(day);
        var handler = new DeleteFoodEntryHandler(dayRepo, SignedInUser.WithId(day.UserId));

        var result = await handler.Handle(
            new DeleteFoodEntryCommand(day.Entries.Single().Id, day.Version), CancellationToken.None);

        result.Found.ShouldBeTrue();
        result.Day.ShouldBeNull();
        dayRepo.DeleteCount.ShouldBe(1);
        dayRepo.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task Deleting_an_entry_that_does_not_exist_reports_not_found()
    {
        var handler = new DeleteFoodEntryHandler(FakeLoggedDayRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        var result = await handler.Handle(
            new DeleteFoodEntryCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Found.ShouldBeFalse();
    }

    [Fact]
    public async Task Deleting_with_a_stale_version_is_refused()
    {
        var day = LoggedDayBuilder.ADay().Ate(FakeFoodLibraryRepository.Oats()).Build();
        var handler = new DeleteFoodEntryHandler(
            FakeLoggedDayRepository.Containing(day), SignedInUser.WithId(day.UserId));

        await Should.ThrowAsync<ConcurrencyConflictException>(
            handler.Handle(new DeleteFoodEntryCommand(day.Entries.Single().Id, Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Deleting_without_a_signed_in_member_is_refused()
    {
        var handler = new DeleteFoodEntryHandler(FakeLoggedDayRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new DeleteFoodEntryCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    // --- helpers ----------------------------------------------------------

    private static (DietPlanAggregate Plan, FakeDietPlanRepository Repo, Guid UserId) APlan(
        int startedDaysAgo = 30, int calories = 2100)
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(startedDaysAgo).WithTargets(calories);
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }

    private static AddFoodEntryRequest Request(
        FoodItem food,
        decimal quantity = 1m,
        MealType meal = MealType.Breakfast,
        Guid? version = null) =>
        new(food.Id, food.ServingSizes.First().Id, quantity, meal, version);
}
