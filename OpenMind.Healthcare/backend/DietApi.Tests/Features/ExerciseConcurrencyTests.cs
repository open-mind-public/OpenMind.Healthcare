using DietApi.Domain;
using DietApi.Features.Exercise;
using DietApi.Features.Exercise.AddExerciseEntry;
using DietApi.Features.Exercise.DeleteExerciseEntry;
using DietApi.Features.Exercise.UpdateExerciseEntry;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Two tabs on the same day. The write built on the older copy is refused, and nothing is lost.
/// </summary>
/// <remarks>
/// Refused rather than merged, deliberately: merging would silently resurrect a session the
/// member deleted in the other tab. A 409 tells them their copy is out of date, which is true,
/// and leaves the stored day exactly as the other tab left it (FR-012).
/// </remarks>
public class ExerciseConcurrencyTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task A_second_add_built_on_a_stale_version_is_refused_and_the_first_survives()
    {
        var (plan, planRepo, userId) = APlan();
        var running = FakeActivityTypeRepository.Running();
        var walk = FakeActivityTypeRepository.BriskWalk();
        var days = FakeExerciseDayRepository.Empty();

        var handler = new AddExerciseEntryHandler(
            planRepo, days, FakeActivityTypeRepository.Containing(running, walk), SignedInUser.WithId(userId));

        // Tab A records a run and holds the version it got back.
        var afterFirst = await handler.Handle(
            new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(running.Id, 45, null)),
            CancellationToken.None);

        var staleVersion = afterFirst!.Version;

        // Tab A also records a walk, moving the day on.
        await handler.Handle(
            new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(walk.Id, 30, staleVersion)),
            CancellationToken.None);

        // Tab B, which never reloaded, tries to write against the version it still holds.
        await Should.ThrowAsync<ConcurrencyConflictException>(
            handler.Handle(
                new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(running.Id, 20, staleVersion)),
                CancellationToken.None));

        // Both of tab A's sessions are still there, and tab B's write did not land.
        var stored = days.Stored.Single();
        stored.Entries.Count.ShouldBe(2);
        stored.Totals.Minutes.ShouldBe(75);
        plan.ShouldNotBeNull();
    }

    [Fact]
    public async Task An_edit_built_on_a_stale_version_is_refused()
    {
        var (plan, planRepo, userId) = APlan();
        var running = FakeActivityTypeRepository.Running();
        var day = ExerciseDayBuilder.ADay().ForUser(userId).ForPlan(plan.Id).Did(running, 45).Build();
        var days = FakeExerciseDayRepository.Containing(day);

        var stale = Guid.NewGuid();

        var handler = new UpdateExerciseEntryHandler(
            planRepo, days, FakeActivityTypeRepository.Containing(running), SignedInUser.WithId(userId));

        await Should.ThrowAsync<ConcurrencyConflictException>(
            handler.Handle(
                new UpdateExerciseEntryCommand(
                    day.Entries.Single().Id, new UpdateExerciseEntryRequest(running.Id, 90, stale)),
                CancellationToken.None));

        days.Stored.Single().Entries.Single().DurationMinutes.ShouldBe(45);
    }

    [Fact]
    public async Task A_delete_built_on_a_stale_version_is_refused_and_the_session_survives()
    {
        var (plan, _, userId) = APlan();
        var day = ExerciseDayBuilder.ADay()
            .ForUser(userId).ForPlan(plan.Id)
            .Did(FakeActivityTypeRepository.Running(), 45)
            .Build();

        var days = FakeExerciseDayRepository.Containing(day);
        var handler = new DeleteExerciseEntryHandler(days, SignedInUser.WithId(userId));

        await Should.ThrowAsync<ConcurrencyConflictException>(
            handler.Handle(
                new DeleteExerciseEntryCommand(day.Entries.Single().Id, Guid.NewGuid()),
                CancellationToken.None));

        days.Stored.Single().Entries.Count.ShouldBe(1);
        days.DeleteCount.ShouldBe(0);
    }

    [Fact]
    public async Task The_current_version_is_accepted()
    {
        var (plan, planRepo, userId) = APlan();
        var running = FakeActivityTypeRepository.Running();
        var day = ExerciseDayBuilder.ADay().ForUser(userId).ForPlan(plan.Id).Did(running, 45).Build();
        var days = FakeExerciseDayRepository.Containing(day);

        var handler = new UpdateExerciseEntryHandler(
            planRepo, days, FakeActivityTypeRepository.Containing(running), SignedInUser.WithId(userId));

        var updated = await handler.Handle(
            new UpdateExerciseEntryCommand(
                day.Entries.Single().Id, new UpdateExerciseEntryRequest(running.Id, 90, day.Version)),
            CancellationToken.None);

        updated.ShouldNotBeNull();
        updated.TotalMinutes.ShouldBe(90);

        // And the token moved on, so the copy that just succeeded is itself now stale.
        updated.Version.ShouldNotBeNull();
    }

    private static (DietApi.Domain.Aggregates.DietPlan Plan, FakeDietPlanRepository Repo, Guid UserId) APlan()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30).Weighing(70m);
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }
}
