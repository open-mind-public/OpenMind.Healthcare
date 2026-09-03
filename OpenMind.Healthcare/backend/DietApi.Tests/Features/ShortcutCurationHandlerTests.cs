using DDD.BuildingBlocks;
using DietApi.Domain;
using DietApi.Features.Exercise;
using DietApi.Features.Exercise.AddEntryFromShortcut;
using DietApi.Features.ExerciseShortcuts;
using DietApi.Features.ExerciseShortcuts.DeleteShortcut;
using DietApi.Features.ExerciseShortcuts.RenameShortcut;
using DietApi.Features.ExerciseShortcuts.ReorderShortcuts;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Slice tests for curating the list, and for the rules the one-tap path must not relax.
/// </summary>
public class ShortcutCurationHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // --- Rename, reorder, remove -------------------------------------------

    [Fact]
    public async Task Renaming_changes_the_name_and_persists()
    {
        var (plan, planRepo, userId, running) = APlanWithShortcuts();
        var id = plan.ShortcutsInOrder()[0].Id;

        var response = await new RenameShortcutHandler(planRepo, Builder(running), SignedInUser.WithId(userId))
            .Handle(new RenameShortcutCommand(id, new RenameShortcutRequest("Park run")), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Shortcuts.Single(s => s.Id == id).Name.ShouldBe("Park run");
        planRepo.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Reordering_puts_them_where_the_member_asked()
    {
        var (plan, planRepo, userId, running) = APlanWithShortcuts();
        var ordered = plan.ShortcutsInOrder().Select(s => s.Id).Reverse().ToList();

        var response = await new ReorderShortcutsHandler(planRepo, Builder(running), SignedInUser.WithId(userId))
            .Handle(new ReorderShortcutsCommand(new ReorderShortcutsRequest(ordered)), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Shortcuts.Select(s => s.Id).ShouldBe(ordered);
        response.Shortcuts.Select(s => s.Position).ShouldBe([0, 1]);
    }

    [Fact]
    public async Task A_reorder_that_does_not_cover_every_shortcut_is_refused()
    {
        var (plan, planRepo, userId, running) = APlanWithShortcuts();

        await Should.ThrowAsync<DomainException>(
            new ReorderShortcutsHandler(planRepo, Builder(running), SignedInUser.WithId(userId))
                .Handle(
                    new ReorderShortcutsCommand(new ReorderShortcutsRequest([plan.ShortcutsInOrder()[0].Id])),
                    CancellationToken.None));
    }

    [Fact]
    public async Task A_reorder_naming_a_shortcut_the_member_does_not_own_is_refused()
    {
        var (plan, planRepo, userId, running) = APlanWithShortcuts();

        await Should.ThrowAsync<DomainException>(
            new ReorderShortcutsHandler(planRepo, Builder(running), SignedInUser.WithId(userId))
                .Handle(
                    new ReorderShortcutsCommand(
                        new ReorderShortcutsRequest([plan.ShortcutsInOrder()[0].Id, Guid.NewGuid()])),
                    CancellationToken.None));
    }

    [Fact]
    public async Task Removing_takes_it_out_and_closes_the_gap()
    {
        var (plan, planRepo, userId, running) = APlanWithShortcuts();
        var first = plan.ShortcutsInOrder()[0].Id;

        var response = await new DeleteShortcutHandler(planRepo, Builder(running), SignedInUser.WithId(userId))
            .Handle(new DeleteShortcutCommand(first), CancellationToken.None);

        response.ShouldNotBeNull();
        response.Shortcuts.Count.ShouldBe(1);
        response.Shortcuts.Single().Position.ShouldBe(0);
        response.RemainingSlots.ShouldBe(DietApi.Domain.Aggregates.DietPlan.MaxShortcuts - 1);
    }

    [Fact]
    public async Task A_shortcut_that_is_not_the_callers_gives_null_so_the_endpoint_can_answer_404()
    {
        var (_, planRepo, userId, running) = APlanWithShortcuts();

        (await new RenameShortcutHandler(planRepo, Builder(running), SignedInUser.WithId(userId))
            .Handle(new RenameShortcutCommand(Guid.NewGuid(), new RenameShortcutRequest("Nope")),
                CancellationToken.None))
            .ShouldBeNull();

        (await new DeleteShortcutHandler(planRepo, Builder(running), SignedInUser.WithId(userId))
            .Handle(new DeleteShortcutCommand(Guid.NewGuid()), CancellationToken.None))
            .ShouldBeNull();
    }

    [Fact]
    public async Task Curating_without_a_signed_in_member_is_refused()
    {
        var planRepo = FakeDietPlanRepository.Empty();

        await Should.ThrowAsync<UnauthorizedAccessException>(
            new RenameShortcutHandler(planRepo, Builder(), SignedInUser.Anonymous())
                .Handle(new RenameShortcutCommand(Guid.NewGuid(), new RenameShortcutRequest("x")),
                    CancellationToken.None));

        await Should.ThrowAsync<UnauthorizedAccessException>(
            new DeleteShortcutHandler(planRepo, Builder(), SignedInUser.Anonymous())
                .Handle(new DeleteShortcutCommand(Guid.NewGuid()), CancellationToken.None));

        await Should.ThrowAsync<UnauthorizedAccessException>(
            new ReorderShortcutsHandler(planRepo, Builder(), SignedInUser.Anonymous())
                .Handle(new ReorderShortcutsCommand(new ReorderShortcutsRequest([])),
                    CancellationToken.None));
    }

    // --- Curating never touches recorded sessions ---------------------------

    [Fact]
    public async Task Renaming_reordering_and_deleting_leave_recorded_sessions_untouched()
    {
        // A shortcut is a button, not a record. The sessions carry their own snapshots and have no
        // link back to the shortcut that produced them (FR-017, SC-009).
        var (plan, planRepo, userId, running) = APlanWithShortcuts();
        var days = FakeExerciseDayRepository.Empty();
        var catalogue = FakeActivityTypeRepository.Containing(running);
        var shortcutId = plan.ShortcutsInOrder()[0].Id;

        await new AddEntryFromShortcutHandler(planRepo, days, catalogue, SignedInUser.WithId(userId))
            .Handle(
                new AddEntryFromShortcutCommand(Today, new AddEntryFromShortcutRequest(shortcutId, null)),
                CancellationToken.None);

        var before = Snapshot(days);

        await new RenameShortcutHandler(planRepo, Builder(running), SignedInUser.WithId(userId))
            .Handle(new RenameShortcutCommand(shortcutId, new RenameShortcutRequest("Renamed")),
                CancellationToken.None);

        await new ReorderShortcutsHandler(planRepo, Builder(running), SignedInUser.WithId(userId))
            .Handle(
                new ReorderShortcutsCommand(
                    new ReorderShortcutsRequest([.. plan.ShortcutsInOrder().Select(s => s.Id).Reverse()])),
                CancellationToken.None);

        await new DeleteShortcutHandler(planRepo, Builder(running), SignedInUser.WithId(userId))
            .Handle(new DeleteShortcutCommand(shortcutId), CancellationToken.None);

        Snapshot(days).ShouldBe(before);
        days.Stored.Single().Entries.Count.ShouldBe(1);
    }

    // --- The one-tap path relaxes nothing -----------------------------------

    [Fact]
    public async Task Tapping_on_a_future_date_is_refused()
    {
        var (plan, planRepo, userId, running) = APlanWithShortcuts();

        await Should.ThrowAsync<DomainException>(
            TapHandler(planRepo, FakeExerciseDayRepository.Empty(), userId, running)
                .Handle(
                    new AddEntryFromShortcutCommand(
                        Today.AddDays(1),
                        new AddEntryFromShortcutRequest(plan.ShortcutsInOrder()[0].Id, null)),
                    CancellationToken.None));
    }

    [Fact]
    public async Task Tapping_on_a_date_before_the_plan_started_is_refused()
    {
        var running = FakeActivityTypeRepository.Running();
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(10).WithShortcut(running.Id, 45, "Run");
        var plan = builder.Build();

        await Should.ThrowAsync<DomainException>(
            TapHandler(FakeDietPlanRepository.Containing(plan), FakeExerciseDayRepository.Empty(),
                    builder.UserId, running)
                .Handle(
                    new AddEntryFromShortcutCommand(
                        Today.AddDays(-11),
                        new AddEntryFromShortcutRequest(plan.ShortcutsInOrder()[0].Id, null)),
                    CancellationToken.None));
    }

    [Fact]
    public async Task Tapping_with_a_stale_day_version_is_refused_and_nothing_is_lost()
    {
        var (plan, planRepo, userId, running) = APlanWithShortcuts();
        var days = FakeExerciseDayRepository.Empty();
        var shortcutId = plan.ShortcutsInOrder()[0].Id;
        var handler = TapHandler(planRepo, days, userId, running);

        var first = await handler.Handle(
            new AddEntryFromShortcutCommand(Today, new AddEntryFromShortcutRequest(shortcutId, null)),
            CancellationToken.None);

        var stale = first!.Version;

        await handler.Handle(
            new AddEntryFromShortcutCommand(Today, new AddEntryFromShortcutRequest(shortcutId, stale)),
            CancellationToken.None);

        await Should.ThrowAsync<ConcurrencyConflictException>(
            handler.Handle(
                new AddEntryFromShortcutCommand(Today, new AddEntryFromShortcutRequest(shortcutId, stale)),
                CancellationToken.None));

        days.Stored.Single().Entries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Tapping_a_shortcut_that_is_not_the_callers_gives_null()
    {
        var (_, planRepo, userId, running) = APlanWithShortcuts();

        (await TapHandler(planRepo, FakeExerciseDayRepository.Empty(), userId, running)
            .Handle(
                new AddEntryFromShortcutCommand(Today, new AddEntryFromShortcutRequest(Guid.NewGuid(), null)),
                CancellationToken.None))
            .ShouldBeNull();
    }

    [Fact]
    public async Task Tapping_a_shortcut_whose_activity_has_left_the_catalogue_gives_null()
    {
        var (plan, planRepo, userId, _) = APlanWithShortcuts();

        (await new AddEntryFromShortcutHandler(
                planRepo, FakeExerciseDayRepository.Empty(), FakeActivityTypeRepository.Empty(),
                SignedInUser.WithId(userId))
            .Handle(
                new AddEntryFromShortcutCommand(
                    Today, new AddEntryFromShortcutRequest(plan.ShortcutsInOrder()[0].Id, null)),
                CancellationToken.None))
            .ShouldBeNull();
    }

    [Fact]
    public async Task Tapping_without_a_signed_in_member_is_refused()
    {
        await Should.ThrowAsync<UnauthorizedAccessException>(
            new AddEntryFromShortcutHandler(
                    FakeDietPlanRepository.Empty(), FakeExerciseDayRepository.Empty(),
                    FakeActivityTypeRepository.Empty(), SignedInUser.Anonymous())
                .Handle(
                    new AddEntryFromShortcutCommand(Today, new AddEntryFromShortcutRequest(Guid.NewGuid(), null)),
                    CancellationToken.None));
    }

    // --- Helpers ----------------------------------------------------------

    private static ShortcutListBuilder Builder(params DietApi.Domain.Aggregates.ActivityType[] activities) =>
        new(FakeActivityTypeRepository.Containing(activities));

    private static AddEntryFromShortcutHandler TapHandler(
        FakeDietPlanRepository planRepo, FakeExerciseDayRepository days, Guid userId,
        DietApi.Domain.Aggregates.ActivityType activity) =>
        new(planRepo, days, FakeActivityTypeRepository.Containing(activity), SignedInUser.WithId(userId));

    private static string Snapshot(FakeExerciseDayRepository days) =>
        string.Join('|', days.Stored.SelectMany(d => d.Entries).Select(e =>
            $"{e.Id}:{e.ActivityName}:{e.Met}:{e.DurationMinutes}:{e.EstimatedKcal}:{e.RecordedAt.Ticks}"));

    private static (DietApi.Domain.Aggregates.DietPlan Plan, FakeDietPlanRepository Repo, Guid UserId,
        DietApi.Domain.Aggregates.ActivityType Activity) APlanWithShortcuts()
    {
        var running = FakeActivityTypeRepository.Running();
        var walk = FakeActivityTypeRepository.BriskWalk();

        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30).Weighing(70m)
            .WithShortcut(running.Id, 45, "Morning run")
            .WithShortcut(walk.Id, 30, "Dog walk");

        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId, running);
    }
}
