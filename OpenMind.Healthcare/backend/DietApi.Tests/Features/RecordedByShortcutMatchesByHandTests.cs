using DietPlanAggregate = DietApi.Domain.Aggregates.DietPlan;
using DietApi.Features.Exercise;
using DietApi.Features.Exercise.AddEntryFromShortcut;
using DietApi.Features.Exercise.AddExerciseEntry;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// A session recorded by tapping a shortcut is the same session, recorded faster.
/// </summary>
/// <remarks>
/// <para>
/// Two guarantees are proven here, and the design rests on both.
/// </para>
/// <para>
/// The first is that the two paths agree: a shortcut is a faster way to reach the same behaviour,
/// not a second implementation of it. Both end in the same aggregate method, and this compares the
/// results field by field rather than trusting that.
/// </para>
/// <para>
/// The second is the one the whole shape of the feature exists to protect. A shortcut stores no
/// estimate, so tapping it after a change of weight produces a figure for the member as they are
/// now. Caching the estimate on the shortcut is the obvious optimisation and it would freeze a
/// member's weight at the moment they saved the button — a member who lost ten kilograms would go
/// on getting estimates for the person they used to be, from a control that gives no hint it is
/// stale (FR-010, SC-002, SC-003).
/// </para>
/// </remarks>
public class RecordedByShortcutMatchesByHandTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task The_two_paths_produce_identical_entries()
    {
        var running = FakeActivityTypeRepository.Running();
        var catalogue = FakeActivityTypeRepository.Containing(running);

        var (planByHand, byHandRepo, byHandUser) = APlan(running, minutes: 45);
        var (planByTap, byTapRepo, byTapUser) = APlan(running, minutes: 45);

        var byHandDays = FakeExerciseDayRepository.Empty();
        var byTapDays = FakeExerciseDayRepository.Empty();

        await new AddExerciseEntryHandler(byHandRepo, byHandDays, catalogue, SignedInUser.WithId(byHandUser))
            .Handle(
                new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(running.Id, 45, null)),
                CancellationToken.None);

        await new AddEntryFromShortcutHandler(byTapRepo, byTapDays, catalogue, SignedInUser.WithId(byTapUser))
            .Handle(
                new AddEntryFromShortcutCommand(
                    Today, new AddEntryFromShortcutRequest(planByTap.ShortcutsInOrder()[0].Id, null)),
                CancellationToken.None);

        var typed = byHandDays.Stored.Single().Entries.Single();
        var tapped = byTapDays.Stored.Single().Entries.Single();

        tapped.ActivityTypeId.ShouldBe(typed.ActivityTypeId);
        tapped.ActivityName.ShouldBe(typed.ActivityName);
        tapped.Met.ShouldBe(typed.Met);
        tapped.DurationMinutes.ShouldBe(typed.DurationMinutes);
        tapped.EstimatedKcal.ShouldBe(typed.EstimatedKcal);

        byTapDays.Stored.Single().Totals.Minutes.ShouldBe(byHandDays.Stored.Single().Totals.Minutes);
        byTapDays.Stored.Single().Totals.Kilocalories
            .ShouldBe(byHandDays.Stored.Single().Totals.Kilocalories);

        planByHand.ShouldNotBeNull();
    }

    [Fact]
    public async Task The_estimate_follows_the_members_current_weight_not_the_weight_when_it_was_saved()
    {
        var running = FakeActivityTypeRepository.Running();
        var catalogue = FakeActivityTypeRepository.Containing(running);

        // Saved at 70 kg.
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30).Weighing(70m)
            .WithShortcut(running.Id, 45, "Morning run");
        var plan = builder.Build();
        var planRepo = FakeDietPlanRepository.Containing(plan);
        var days = FakeExerciseDayRepository.Empty();
        var shortcutId = plan.ShortcutsInOrder()[0].Id;

        var handler = new AddEntryFromShortcutHandler(
            planRepo, days, catalogue, SignedInUser.WithId(builder.UserId));

        var first = await handler.Handle(
            new AddEntryFromShortcutCommand(Today, new AddEntryFromShortcutRequest(shortcutId, null)),
            CancellationToken.None);

        first.ShouldNotBeNull();
        var atSeventy = first.Entries.Single().EstimatedKcal;
        atSeventy.ShouldBe(436);

        // The member weighs themselves and has lost weight.
        plan.RecordWeight(Today, 60m);

        var second = await handler.Handle(
            new AddEntryFromShortcutCommand(
                Today, new AddEntryFromShortcutRequest(shortcutId, first.Version)),
            CancellationToken.None);

        second.ShouldNotBeNull();
        var estimates = second.Entries.Select(e => e.EstimatedKcal).ToList();

        // The new session used the weight the member is now.
        estimates.Count.ShouldBe(2);
        estimates[1].ShouldBe(374);

        // And the one already recorded is exactly as it was.
        estimates[0].ShouldBe(atSeventy);
    }

    [Fact]
    public async Task A_corrected_energy_rate_reaches_the_next_tap_but_not_the_last_one()
    {
        var running = FakeActivityTypeRepository.Running();

        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30).Weighing(70m)
            .WithShortcut(running.Id, 45, "Morning run");
        var plan = builder.Build();
        var planRepo = FakeDietPlanRepository.Containing(plan);
        var days = FakeExerciseDayRepository.Empty();
        var shortcutId = plan.ShortcutsInOrder()[0].Id;

        var first = await new AddEntryFromShortcutHandler(
                planRepo, days, FakeActivityTypeRepository.Containing(running),
                SignedInUser.WithId(builder.UserId))
            .Handle(
                new AddEntryFromShortcutCommand(Today, new AddEntryFromShortcutRequest(shortcutId, null)),
                CancellationToken.None);

        first.ShouldNotBeNull();
        var beforeCorrection = first.Entries.Single().EstimatedKcal;

        // The catalogue is corrected. The shortcut points at the activity, so the next tap picks
        // up the new figure - which is the point of storing a reference rather than a copy.
        var corrected = DietApi.Domain.Aggregates.ActivityType.Create(
            "Running, 8 km/h", DietApi.Domain.ValueObjects.ActivityCategory.Running, 6.0m);
        typeof(DietApi.Domain.Aggregates.ActivityType)
            .GetProperty(nameof(DietApi.Domain.Aggregates.ActivityType.Id))!
            .SetValue(corrected, running.Id);

        var second = await new AddEntryFromShortcutHandler(
                planRepo, days, FakeActivityTypeRepository.Containing(corrected),
                SignedInUser.WithId(builder.UserId))
            .Handle(
                new AddEntryFromShortcutCommand(
                    Today, new AddEntryFromShortcutRequest(shortcutId, first.Version)),
                CancellationToken.None);

        second.ShouldNotBeNull();
        var estimates = second.Entries.Select(e => e.EstimatedKcal).ToList();

        estimates[1].ShouldBeLessThan(beforeCorrection);
        estimates[0].ShouldBe(beforeCorrection);
    }

    [Fact]
    public void A_shortcut_stores_no_figure_that_could_go_stale()
    {
        // Asserted structurally, so a later "just cache the estimate for speed" change fails here
        // rather than silently freezing a member's weight.
        var properties = typeof(DietApi.Domain.Entities.ExerciseShortcut)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        properties.ShouldNotContain("Met");
        properties.ShouldNotContain("EstimatedKcal");
        properties.ShouldNotContain("ActivityName");
        properties.ShouldNotContain("WeightKg");

        properties.ShouldContain(nameof(DietApi.Domain.Entities.ExerciseShortcut.ActivityTypeId));
    }

    private static (DietPlanAggregate Plan, FakeDietPlanRepository Repo, Guid UserId) APlan(
        DietApi.Domain.Aggregates.ActivityType activity, int minutes)
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30).Weighing(70m)
            .WithShortcut(activity.Id, minutes, "Morning run");
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }
}
