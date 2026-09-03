using DDD.BuildingBlocks;
using DietPlanAggregate = DietApi.Domain.Aggregates.DietPlan;
using ActivityTypeAggregate = DietApi.Domain.Aggregates.ActivityType;
using DietApi.Domain;
using DietApi.Features.ActivityCatalogue.SearchActivities;
using DietApi.Features.Exercise;
using DietApi.Features.Exercise.AddExerciseEntry;
using DietApi.Features.Exercise.DeleteExerciseEntry;
using DietApi.Features.Exercise.GetExerciseDay;
using DietApi.Features.Exercise.UpdateExerciseEntry;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for recording exercise and searching the catalogue.
/// </summary>
public class ExerciseHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // --- Get day ----------------------------------------------------------

    [Fact]
    public async Task A_date_with_nothing_recorded_returns_an_empty_day_rather_than_a_404()
    {
        var (_, planRepo, userId) = APlan();
        var handler = new GetExerciseDayHandler(
            planRepo, FakeExerciseDayRepository.Empty(), SignedInUser.WithId(userId));

        var day = await handler.Handle(new GetExerciseDayQuery(Today), CancellationToken.None);

        day.ShouldNotBeNull();
        day.TotalMinutes.ShouldBe(0);
        day.TotalKilocalories.ShouldBe(0);
        day.Version.ShouldBeNull();
        day.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_member_with_no_plan_gets_null_so_the_endpoint_can_answer_404()
    {
        var handler = new GetExerciseDayHandler(
            FakeDietPlanRepository.Empty(), FakeExerciseDayRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        (await handler.Handle(new GetExerciseDayQuery(Today), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task A_recorded_day_comes_back_with_its_sessions_and_totals()
    {
        var (plan, planRepo, userId) = APlan();
        var running = FakeActivityTypeRepository.Running();

        var day = ExerciseDayBuilder.ADay().ForUser(userId).ForPlan(plan.Id).Did(running, 45).Build();

        var handler = new GetExerciseDayHandler(
            planRepo, FakeExerciseDayRepository.Containing(day), SignedInUser.WithId(userId));

        var dto = await handler.Handle(new GetExerciseDayQuery(Today), CancellationToken.None);

        dto.ShouldNotBeNull();
        dto.Entries.Count.ShouldBe(1);
        dto.Entries[0].ActivityName.ShouldBe(running.Name);
        dto.TotalMinutes.ShouldBe(45);
        dto.Version.ShouldBe(day.Version);
    }

    [Fact]
    public async Task A_future_date_is_refused()
    {
        var (_, planRepo, userId) = APlan();
        var handler = new GetExerciseDayHandler(
            planRepo, FakeExerciseDayRepository.Empty(), SignedInUser.WithId(userId));

        await Should.ThrowAsync<DomainException>(
            handler.Handle(new GetExerciseDayQuery(Today.AddDays(1)), CancellationToken.None));
    }

    [Fact]
    public async Task A_date_before_the_plan_started_is_refused()
    {
        var (_, planRepo, userId) = APlan(startedDaysAgo: 10);
        var handler = new GetExerciseDayHandler(
            planRepo, FakeExerciseDayRepository.Empty(), SignedInUser.WithId(userId));

        await Should.ThrowAsync<DomainException>(
            handler.Handle(new GetExerciseDayQuery(Today.AddDays(-11)), CancellationToken.None));
    }

    [Fact]
    public async Task Fetching_a_day_without_a_signed_in_member_is_refused()
    {
        var handler = new GetExerciseDayHandler(
            FakeDietPlanRepository.Empty(), FakeExerciseDayRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new GetExerciseDayQuery(Today), CancellationToken.None));
    }

    [Fact]
    public async Task Another_members_day_is_not_returned()
    {
        var (plan, planRepo, userId) = APlan();
        var someoneElse = ExerciseDayBuilder.ADay()
            .ForUser(Guid.NewGuid())
            .Did(FakeActivityTypeRepository.Running(), 45)
            .Build();

        var handler = new GetExerciseDayHandler(
            planRepo, FakeExerciseDayRepository.Containing(someoneElse), SignedInUser.WithId(userId));

        var day = await handler.Handle(new GetExerciseDayQuery(Today), CancellationToken.None);

        day.ShouldNotBeNull();
        day.Entries.ShouldBeEmpty();
        plan.ShouldNotBeNull();
    }

    // --- Add entry --------------------------------------------------------

    [Fact]
    public async Task Recording_a_session_creates_the_day_and_returns_it_with_an_estimate()
    {
        var (_, planRepo, userId) = APlan(weightKg: 70m);
        var running = FakeActivityTypeRepository.Running();
        var days = FakeExerciseDayRepository.Empty();
        var handler = AddHandler(planRepo, days, userId, running);

        var day = await handler.Handle(
            new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(running.Id, 45, null)),
            CancellationToken.None);

        day.ShouldNotBeNull();
        day.TotalMinutes.ShouldBe(45);
        day.Entries.Single().EstimatedKcal.ShouldBe(436);
        day.Version.ShouldNotBeNull();
        days.SaveCount.ShouldBe(1);
        days.Stored.Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_second_session_the_same_day_is_added_beside_the_first()
    {
        var (_, planRepo, userId) = APlan();
        var running = FakeActivityTypeRepository.Running();
        var walk = FakeActivityTypeRepository.BriskWalk();
        var days = FakeExerciseDayRepository.Empty();
        var handler = AddHandler(planRepo, days, userId, running, walk);

        var first = await handler.Handle(
            new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(running.Id, 45, null)),
            CancellationToken.None);

        var second = await handler.Handle(
            new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(walk.Id, 30, first!.Version)),
            CancellationToken.None);

        second.ShouldNotBeNull();
        second.Entries.Count.ShouldBe(2);
        second.TotalMinutes.ShouldBe(75);
        days.Stored.Count.ShouldBe(1);
    }

    [Fact]
    public async Task An_activity_that_is_not_in_the_catalogue_gives_null_so_the_endpoint_can_answer_404()
    {
        var (_, planRepo, userId) = APlan();
        var handler = AddHandler(planRepo, FakeExerciseDayRepository.Empty(), userId);

        var day = await handler.Handle(
            new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(Guid.NewGuid(), 45, null)),
            CancellationToken.None);

        day.ShouldBeNull();
    }

    [Fact]
    public async Task Recording_exercise_without_a_plan_is_refused()
    {
        var running = FakeActivityTypeRepository.Running();
        var handler = new AddExerciseEntryHandler(
            FakeDietPlanRepository.Empty(),
            FakeExerciseDayRepository.Empty(),
            FakeActivityTypeRepository.Containing(running),
            SignedInUser.WithId(Guid.NewGuid()));

        await Should.ThrowAsync<DomainException>(
            handler.Handle(
                new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(running.Id, 45, null)),
                CancellationToken.None));
    }

    [Fact]
    public async Task A_session_on_a_future_date_is_refused()
    {
        var (_, planRepo, userId) = APlan();
        var running = FakeActivityTypeRepository.Running();
        var handler = AddHandler(planRepo, FakeExerciseDayRepository.Empty(), userId, running);

        await Should.ThrowAsync<DomainException>(
            handler.Handle(
                new AddExerciseEntryCommand(Today.AddDays(1), new AddExerciseEntryRequest(running.Id, 45, null)),
                CancellationToken.None));
    }

    [Fact]
    public async Task A_session_before_the_plan_started_is_refused()
    {
        var (_, planRepo, userId) = APlan(startedDaysAgo: 10);
        var running = FakeActivityTypeRepository.Running();
        var handler = AddHandler(planRepo, FakeExerciseDayRepository.Empty(), userId, running);

        await Should.ThrowAsync<DomainException>(
            handler.Handle(
                new AddExerciseEntryCommand(Today.AddDays(-11), new AddExerciseEntryRequest(running.Id, 45, null)),
                CancellationToken.None));
    }

    [Fact]
    public async Task A_session_of_no_duration_is_refused()
    {
        var (_, planRepo, userId) = APlan();
        var running = FakeActivityTypeRepository.Running();
        var days = FakeExerciseDayRepository.Empty();
        var handler = AddHandler(planRepo, days, userId, running);

        await Should.ThrowAsync<DomainException>(
            handler.Handle(
                new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(running.Id, 0, null)),
                CancellationToken.None));

        days.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task Recording_a_session_without_a_signed_in_member_is_refused()
    {
        var handler = new AddExerciseEntryHandler(
            FakeDietPlanRepository.Empty(),
            FakeExerciseDayRepository.Empty(),
            FakeActivityTypeRepository.Empty(),
            SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(
                new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(Guid.NewGuid(), 45, null)),
                CancellationToken.None));
    }

    // --- Update entry -----------------------------------------------------

    [Fact]
    public async Task Changing_a_sessions_duration_re_estimates_and_updates_the_day()
    {
        var (plan, planRepo, userId) = APlan();
        var running = FakeActivityTypeRepository.Running();
        var day = ExerciseDayBuilder.ADay().ForUser(userId).ForPlan(plan.Id).Weighing(70m).Did(running, 45).Build();
        var days = FakeExerciseDayRepository.Containing(day);

        var handler = new UpdateExerciseEntryHandler(
            planRepo, days, FakeActivityTypeRepository.Containing(running), SignedInUser.WithId(userId));

        var updated = await handler.Handle(
            new UpdateExerciseEntryCommand(
                day.Entries.Single().Id, new UpdateExerciseEntryRequest(running.Id, 90, day.Version)),
            CancellationToken.None);

        updated.ShouldNotBeNull();
        updated.TotalMinutes.ShouldBe(90);
        updated.Entries.Single().EstimatedKcal.ShouldBe(872);
    }

    [Fact]
    public async Task Changing_which_activity_a_session_was_re_estimates_it()
    {
        var (plan, planRepo, userId) = APlan();
        var running = FakeActivityTypeRepository.Running();
        var walk = FakeActivityTypeRepository.BriskWalk();
        var day = ExerciseDayBuilder.ADay().ForUser(userId).ForPlan(plan.Id).Weighing(70m).Did(running, 45).Build();
        var days = FakeExerciseDayRepository.Containing(day);

        var handler = new UpdateExerciseEntryHandler(
            planRepo, days, FakeActivityTypeRepository.Containing(running, walk), SignedInUser.WithId(userId));

        var updated = await handler.Handle(
            new UpdateExerciseEntryCommand(
                day.Entries.Single().Id, new UpdateExerciseEntryRequest(walk.Id, 45, day.Version)),
            CancellationToken.None);

        updated.ShouldNotBeNull();
        updated.Entries.Single().ActivityName.ShouldBe(walk.Name);
        updated.Entries.Single().Met.ShouldBe(4.3m);
        updated.Entries.Single().EstimatedKcal.ShouldBe(226);
    }

    [Fact]
    public async Task Another_members_session_cannot_be_edited()
    {
        var (_, planRepo, userId) = APlan();
        var running = FakeActivityTypeRepository.Running();
        var someoneElses = ExerciseDayBuilder.ADay().ForUser(Guid.NewGuid()).Did(running, 45).Build();
        var days = FakeExerciseDayRepository.Containing(someoneElses);

        var handler = new UpdateExerciseEntryHandler(
            planRepo, days, FakeActivityTypeRepository.Containing(running), SignedInUser.WithId(userId));

        var updated = await handler.Handle(
            new UpdateExerciseEntryCommand(
                someoneElses.Entries.Single().Id,
                new UpdateExerciseEntryRequest(running.Id, 90, someoneElses.Version)),
            CancellationToken.None);

        updated.ShouldBeNull();
        days.Stored.Single().Entries.Single().DurationMinutes.ShouldBe(45);
    }

    [Fact]
    public async Task Editing_a_session_that_does_not_exist_gives_null()
    {
        var (_, planRepo, userId) = APlan();
        var running = FakeActivityTypeRepository.Running();

        var handler = new UpdateExerciseEntryHandler(
            planRepo, FakeExerciseDayRepository.Empty(),
            FakeActivityTypeRepository.Containing(running), SignedInUser.WithId(userId));

        var updated = await handler.Handle(
            new UpdateExerciseEntryCommand(
                Guid.NewGuid(), new UpdateExerciseEntryRequest(running.Id, 90, Guid.NewGuid())),
            CancellationToken.None);

        updated.ShouldBeNull();
    }

    [Fact]
    public async Task Editing_a_session_without_a_signed_in_member_is_refused()
    {
        var handler = new UpdateExerciseEntryHandler(
            FakeDietPlanRepository.Empty(), FakeExerciseDayRepository.Empty(),
            FakeActivityTypeRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(
                new UpdateExerciseEntryCommand(
                    Guid.NewGuid(), new UpdateExerciseEntryRequest(Guid.NewGuid(), 30, Guid.NewGuid())),
                CancellationToken.None));
    }

    // --- Delete entry -----------------------------------------------------

    [Fact]
    public async Task Removing_a_session_leaves_the_others_and_adjusts_the_total()
    {
        var (plan, _, userId) = APlan();
        var day = ExerciseDayBuilder.ADay()
            .ForUser(userId).ForPlan(plan.Id)
            .Did(FakeActivityTypeRepository.Running(), 45)
            .Did(FakeActivityTypeRepository.BriskWalk(), 30)
            .Build();

        var days = FakeExerciseDayRepository.Containing(day);
        var handler = new DeleteExerciseEntryHandler(days, SignedInUser.WithId(userId));

        var result = await handler.Handle(
            new DeleteExerciseEntryCommand(day.EntriesInOrder().First().Id, day.Version),
            CancellationToken.None);

        result.Found.ShouldBeTrue();
        result.Day.ShouldNotBeNull();
        result.Day.Entries.Count.ShouldBe(1);
        result.Day.TotalMinutes.ShouldBe(30);
    }

    [Fact]
    public async Task Removing_a_session_that_does_not_exist_reports_not_found()
    {
        var handler = new DeleteExerciseEntryHandler(
            FakeExerciseDayRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        var result = await handler.Handle(
            new DeleteExerciseEntryCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Found.ShouldBeFalse();
        result.Day.ShouldBeNull();
    }

    [Fact]
    public async Task Another_members_session_cannot_be_removed()
    {
        var (_, _, userId) = APlan();
        var someoneElses = ExerciseDayBuilder.ADay()
            .ForUser(Guid.NewGuid())
            .Did(FakeActivityTypeRepository.Running(), 45)
            .Build();

        var days = FakeExerciseDayRepository.Containing(someoneElses);
        var handler = new DeleteExerciseEntryHandler(days, SignedInUser.WithId(userId));

        var result = await handler.Handle(
            new DeleteExerciseEntryCommand(someoneElses.Entries.Single().Id, someoneElses.Version),
            CancellationToken.None);

        result.Found.ShouldBeFalse();
        days.Stored.Single().Entries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Removing_a_session_without_a_signed_in_member_is_refused()
    {
        var handler = new DeleteExerciseEntryHandler(
            FakeExerciseDayRepository.Empty(), SignedInUser.Anonymous());

        await Should.ThrowAsync<UnauthorizedAccessException>(
            handler.Handle(new DeleteExerciseEntryCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    // --- Catalogue search -------------------------------------------------

    [Fact]
    public async Task Searching_the_catalogue_returns_matches()
    {
        var handler = new SearchActivitiesHandler(FakeActivityTypeRepository.Containing(
            FakeActivityTypeRepository.Running(),
            FakeActivityTypeRepository.BriskWalk()));

        var result = await handler.Handle(new SearchActivitiesQuery("run"), CancellationToken.None);

        result.Matches.Count.ShouldBe(1);
        result.Matches[0].Name.ShouldBe("Running, 8 km/h");
        result.Matches[0].Met.ShouldBe(8.3m);
    }

    [Fact]
    public async Task An_activity_we_do_not_have_returns_no_matches_rather_than_an_error()
    {
        var handler = new SearchActivitiesHandler(FakeActivityTypeRepository.Containing(
            FakeActivityTypeRepository.Running()));

        var result = await handler.Handle(new SearchActivitiesQuery("quidditch"), CancellationToken.None);

        result.Query.ShouldBe("quidditch");
        result.Matches.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_empty_search_returns_nothing_rather_than_the_whole_catalogue()
    {
        var handler = new SearchActivitiesHandler(FakeActivityTypeRepository.Containing(
            FakeActivityTypeRepository.Running(),
            FakeActivityTypeRepository.BriskWalk()));

        (await handler.Handle(new SearchActivitiesQuery(""), CancellationToken.None)).Matches.ShouldBeEmpty();
    }

    // --- Helpers ----------------------------------------------------------

    private static AddExerciseEntryHandler AddHandler(
        FakeDietPlanRepository planRepo,
        FakeExerciseDayRepository days,
        Guid userId,
        params ActivityTypeAggregate[] activities) =>
        new(planRepo, days, FakeActivityTypeRepository.Containing(activities), SignedInUser.WithId(userId));

    private static (DietPlanAggregate Plan, FakeDietPlanRepository Repo, Guid UserId) APlan(
        int startedDaysAgo = 30, decimal weightKg = 70m)
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(startedDaysAgo).Weighing(weightKg);
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }
}
