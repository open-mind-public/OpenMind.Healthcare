using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// A day holds every session recorded against it. The failure this guards against is the obvious
/// one - treating a date as holding "the" workout, so a member who cycles to work and runs in the
/// evening loses the morning by recording the evening.
/// </summary>
public class MultipleSessionsTests
{
    [Fact]
    public void A_second_session_on_the_same_date_is_added_not_substituted()
    {
        var builder = ExerciseDayBuilder.ADay();
        var running = FakeActivityTypeRepository.Running();
        var walk = FakeActivityTypeRepository.BriskWalk();

        var day = builder.Did(running, 45).Build();
        day.Entries.Count.ShouldBe(1);

        day.AddEntry(walk.Id, walk.Name, walk.Met, 30, builder.WeightKg, builder.Clock);

        day.Entries.Count.ShouldBe(2);
        day.Entries.Select(e => e.ActivityName).ShouldContain(running.Name);
        day.Entries.Select(e => e.ActivityName).ShouldContain(walk.Name);
    }

    [Fact]
    public void The_days_total_is_the_sum_of_its_sessions()
    {
        var builder = ExerciseDayBuilder.ADay().Weighing(70m);
        var day = builder
            .Did(FakeActivityTypeRepository.Running(), 45)
            .Did(FakeActivityTypeRepository.BriskWalk(), 30)
            .Build();

        day.Totals.Minutes.ShouldBe(75);
        day.Totals.Kilocalories.ShouldBe(day.Entries.Sum(e => e.EstimatedKcal));
    }

    [Fact]
    public void The_same_activity_twice_in_a_day_is_two_sessions()
    {
        var builder = ExerciseDayBuilder.ADay();
        var walk = FakeActivityTypeRepository.BriskWalk();

        var day = builder.Did(walk, 20).Did(walk, 25).Build();

        day.Entries.Count.ShouldBe(2);
        day.Totals.Minutes.ShouldBe(45);
    }

    [Fact]
    public void Sessions_are_listed_in_the_order_they_were_recorded()
    {
        var builder = ExerciseDayBuilder.ADay();
        var running = FakeActivityTypeRepository.Running();
        var walk = FakeActivityTypeRepository.BriskWalk();

        var day = builder.Build();

        var morning = day.AddEntry(running.Id, running.Name, running.Met, 45, builder.WeightKg, builder.Clock);
        var evening = day.AddEntry(walk.Id, walk.Name, walk.Met, 30, builder.WeightKg, builder.Clock.AddHours(11));

        day.EntriesInOrder().Select(e => e.Id).ShouldBe([morning.Id, evening.Id]);
    }

    [Fact]
    public void Removing_one_session_leaves_the_others_alone()
    {
        var builder = ExerciseDayBuilder.ADay();
        var day = builder
            .Did(FakeActivityTypeRepository.Running(), 45)
            .Did(FakeActivityTypeRepository.BriskWalk(), 30)
            .Build();

        var surviving = day.EntriesInOrder().Last();

        day.RemoveEntry(day.EntriesInOrder().First().Id).ShouldBeTrue();

        day.Entries.Count.ShouldBe(1);
        day.Entries.Single().Id.ShouldBe(surviving.Id);
        day.IsEmpty.ShouldBeFalse();
    }
}
