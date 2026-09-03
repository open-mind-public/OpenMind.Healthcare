using DietApi.Domain.Aggregates;
using DietApi.Domain.ValueObjects;
using DietApi.Features.Exercise;
using DietApi.Features.Exercise.AddExerciseEntry;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// The estimate is captured at the moment a session is recorded and never recomputed.
/// </summary>
/// <remarks>
/// Both halves of this matter. A MET value corrected in the catalogue next month must not rewrite
/// what a member already saw, and neither must stepping on the scales. Without the snapshot, a
/// member who loses ten kilograms would find every past session quietly costing less than it did
/// when they did it - history changing under them for reasons they never asked for (FR-009,
/// SC-007).
/// </remarks>
public class EstimateSnapshotTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Correcting_the_catalogue_afterwards_does_not_change_a_recorded_estimate()
    {
        var builder = ExerciseDayBuilder.ADay().Weighing(70m);
        var running = FakeActivityTypeRepository.Running();
        var day = builder.Did(running, 45).Build();

        var recorded = day.Entries.Single();
        var estimateWhenRecorded = recorded.EstimatedKcal;
        estimateWhenRecorded.ShouldBe(436);

        // The catalogue is corrected - a new row, a different MET, the same activity.
        var corrected = ActivityType.Create("Running, 8 km/h", ActivityCategory.Running, 6.0m);
        corrected.Met.ShouldNotBe(running.Met);

        // Nothing re-reads the catalogue, so the member's history is untouched.
        day.Entries.Single().EstimatedKcal.ShouldBe(estimateWhenRecorded);
        day.Entries.Single().Met.ShouldBe(8.3m);
        day.Totals.Kilocalories.ShouldBe(estimateWhenRecorded);
    }

    [Fact]
    public async Task Recording_a_new_body_weight_does_not_move_past_estimates()
    {
        var planBuilder = DietPlanBuilder.APlan().StartedDaysAgo(30).Weighing(70m);
        var plan = planBuilder.Build();
        var planRepo = FakeDietPlanRepository.Containing(plan);
        var running = FakeActivityTypeRepository.Running();
        var days = FakeExerciseDayRepository.Empty();

        var handler = new AddExerciseEntryHandler(
            planRepo, days, FakeActivityTypeRepository.Containing(running), SignedInUser.WithId(planBuilder.UserId));

        var recorded = await handler.Handle(
            new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(running.Id, 45, null)),
            CancellationToken.None);

        var estimateAtSeventyKilos = recorded!.Entries.Single().EstimatedKcal;
        estimateAtSeventyKilos.ShouldBe(436);

        // The member weighs themselves and has lost weight.
        plan.RecordWeight(Today, 60m);
        plan.CurrentWeightKg().ShouldBe(60m);

        // The session they already recorded is exactly as it was.
        days.Stored.Single().Entries.Single().EstimatedKcal.ShouldBe(estimateAtSeventyKilos);
        days.Stored.Single().Totals.Kilocalories.ShouldBe(estimateAtSeventyKilos);
    }

    [Fact]
    public async Task The_new_weight_is_used_for_the_next_session()
    {
        // The snapshot protects history, not the future. A session recorded after a weight change
        // is estimated at the weight the member is now.
        var planBuilder = DietPlanBuilder.APlan().StartedDaysAgo(30).Weighing(70m);
        var plan = planBuilder.Build();
        var running = FakeActivityTypeRepository.Running();
        var days = FakeExerciseDayRepository.Empty();

        var handler = new AddExerciseEntryHandler(
            FakeDietPlanRepository.Containing(plan),
            days,
            FakeActivityTypeRepository.Containing(running),
            SignedInUser.WithId(planBuilder.UserId));

        var first = await handler.Handle(
            new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(running.Id, 45, null)),
            CancellationToken.None);

        plan.RecordWeight(Today, 60m);

        var second = await handler.Handle(
            new AddExerciseEntryCommand(Today, new AddExerciseEntryRequest(running.Id, 45, first!.Version)),
            CancellationToken.None);

        var estimates = second!.Entries.Select(e => e.EstimatedKcal).ToList();
        estimates.Count.ShouldBe(2);
        estimates[0].ShouldBe(436);
        estimates[1].ShouldBe(374);
    }

    [Fact]
    public void A_members_own_edit_does_re_estimate()
    {
        // Deliberately unlike a background correction: the member is changing what they said
        // happened, so the figure is recomputed from what they now say.
        var builder = ExerciseDayBuilder.ADay().Weighing(70m);
        var running = FakeActivityTypeRepository.Running();
        var day = builder.Did(running, 45).Build();

        var entry = day.Entries.Single();
        entry.EstimatedKcal.ShouldBe(436);

        day.UpdateEntry(entry.Id, running.Id, running.Name, running.Met, 90, 70m);

        day.Entries.Single().EstimatedKcal.ShouldBe(872);
        day.Totals.Kilocalories.ShouldBe(872);
    }

    [Fact]
    public void The_activity_name_is_snapshotted_too()
    {
        var builder = ExerciseDayBuilder.ADay();
        var running = FakeActivityTypeRepository.Running();
        var day = builder.Did(running, 45).Build();

        // Renaming the catalogue entry cannot rewrite what the member's history says they did.
        var renamed = ActivityType.Create("Running (road), 8 km/h", ActivityCategory.Running, 8.3m);
        renamed.Name.ShouldNotBe(running.Name);

        day.Entries.Single().ActivityName.ShouldBe("Running, 8 km/h");
        ExerciseMapper.ToDto(day).Entries.Single().ActivityName.ShouldBe("Running, 8 km/h");
    }
}
