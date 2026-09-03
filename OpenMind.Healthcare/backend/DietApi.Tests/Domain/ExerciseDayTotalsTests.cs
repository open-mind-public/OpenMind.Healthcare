using DietApi.Domain.Aggregates;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// The stored daily totals duplicate what the sessions already say. That duplication is only safe
/// because the aggregate recomputes it on every mutation - so this asserts the invariant
/// directly, and asserts that the concurrency token moves with it.
/// </summary>
public class ExerciseDayTotalsTests
{
    [Fact]
    public void A_new_day_starts_at_zero()
    {
        var day = ExerciseDayBuilder.ADay().Build();

        day.Totals.Minutes.ShouldBe(0);
        day.Totals.Kilocalories.ShouldBe(0);
        day.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Adding_a_session_keeps_the_totals_equal_to_the_sum_of_sessions()
    {
        var builder = ExerciseDayBuilder.ADay();
        var day = builder.Build();
        var running = FakeActivityTypeRepository.Running();
        var walk = FakeActivityTypeRepository.BriskWalk();

        day.AddEntry(running.Id, running.Name, running.Met, 45, builder.WeightKg, builder.Clock);
        AssertInvariant(day);

        day.AddEntry(walk.Id, walk.Name, walk.Met, 30, builder.WeightKg, builder.Clock);
        AssertInvariant(day);

        day.Totals.Minutes.ShouldBe(75);
    }

    [Fact]
    public void Updating_a_session_keeps_the_totals_equal_to_the_sum_of_sessions()
    {
        var builder = ExerciseDayBuilder.ADay();
        var running = FakeActivityTypeRepository.Running();
        var day = builder.Did(running, 45).Did(FakeActivityTypeRepository.BriskWalk(), 30).Build();

        var entry = day.EntriesInOrder().First();
        day.UpdateEntry(entry.Id, running.Id, running.Name, running.Met, 20, builder.WeightKg);

        AssertInvariant(day);
        day.Totals.Minutes.ShouldBe(50);
    }

    [Fact]
    public void Removing_a_session_keeps_the_totals_equal_to_the_sum_of_sessions()
    {
        var builder = ExerciseDayBuilder.ADay();
        var day = builder
            .Did(FakeActivityTypeRepository.Running(), 45)
            .Did(FakeActivityTypeRepository.BriskWalk(), 30)
            .Build();

        day.RemoveEntry(day.EntriesInOrder().First().Id);

        AssertInvariant(day);
        day.Totals.Minutes.ShouldBe(30);
    }

    [Fact]
    public void Every_mutation_reassigns_the_concurrency_token()
    {
        var builder = ExerciseDayBuilder.ADay();
        var running = FakeActivityTypeRepository.Running();
        var day = builder.Build();

        var atStart = day.Version;

        day.AddEntry(running.Id, running.Name, running.Met, 45, builder.WeightKg, builder.Clock);
        var afterAdd = day.Version;
        afterAdd.ShouldNotBe(atStart);

        var entryId = day.Entries.Single().Id;

        day.UpdateEntry(entryId, running.Id, running.Name, running.Met, 30, builder.WeightKg);
        var afterUpdate = day.Version;
        afterUpdate.ShouldNotBe(afterAdd);

        day.RemoveEntry(entryId);
        day.Version.ShouldNotBe(afterUpdate);
    }

    [Fact]
    public void Removing_a_session_that_is_not_on_the_day_changes_nothing()
    {
        var builder = ExerciseDayBuilder.ADay();
        var day = builder.Did(FakeActivityTypeRepository.Running(), 45).Build();
        var before = day.Version;

        day.RemoveEntry(Guid.NewGuid()).ShouldBeFalse();

        day.Version.ShouldBe(before);
        day.Entries.Count.ShouldBe(1);
    }

    private static void AssertInvariant(ExerciseDay day)
    {
        day.Totals.Minutes.ShouldBe(day.Entries.Sum(e => e.DurationMinutes));
        day.Totals.Kilocalories.ShouldBe(day.Entries.Sum(e => e.EstimatedKcal));
    }
}
