using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// Removing the last session leaves nothing behind.
/// </summary>
/// <remarks>
/// The distinction this protects is between "did no exercise" and "recorded a day of no
/// exercise". A day left standing with zero minutes would show up in the calendar as a day with
/// activity, and would count toward active days in the weekly summary - a member who deleted a
/// mistake would be credited for it.
/// </remarks>
public class EmptyExerciseDayTests
{
    [Fact]
    public void Removing_the_last_session_leaves_the_day_empty()
    {
        var builder = ExerciseDayBuilder.ADay();
        var day = builder.Did(FakeActivityTypeRepository.Running(), 45).Build();

        day.IsEmpty.ShouldBeFalse();

        day.RemoveEntry(day.Entries.Single().Id).ShouldBeTrue();

        day.IsEmpty.ShouldBeTrue();
        day.Totals.Minutes.ShouldBe(0);
        day.Totals.Kilocalories.ShouldBe(0);
    }

    [Fact]
    public async Task An_emptied_day_is_deleted_and_the_date_reports_no_exercise()
    {
        var planBuilder = DietPlanBuilder.APlan().StartedDaysAgo(30);
        var plan = planBuilder.Build();
        var planRepo = FakeDietPlanRepository.Containing(plan);
        var userId = planBuilder.UserId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var running = FakeActivityTypeRepository.Running();
        var day = ExerciseDayBuilder.ADay().ForUser(userId).ForPlan(plan.Id).Did(running, 45).Build();
        var days = FakeExerciseDayRepository.Containing(day);

        var delete = new DietApi.Features.Exercise.DeleteExerciseEntry.DeleteExerciseEntryHandler(
            days, SignedInUser.WithId(userId));

        var result = await delete.Handle(
            new DietApi.Features.Exercise.DeleteExerciseEntry.DeleteExerciseEntryCommand(
                day.Entries.Single().Id, day.Version),
            CancellationToken.None);

        result.Found.ShouldBeTrue();
        result.Day.ShouldBeNull();
        days.DeleteCount.ShouldBe(1);
        days.Stored.ShouldBeEmpty();

        // Reading the date back gives an empty day with no version - the same shape as a date
        // that never had anything recorded on it.
        var get = new DietApi.Features.Exercise.GetExerciseDay.GetExerciseDayHandler(
            planRepo, days, SignedInUser.WithId(userId));

        var after = await get.Handle(
            new DietApi.Features.Exercise.GetExerciseDay.GetExerciseDayQuery(today), CancellationToken.None);

        after.ShouldNotBeNull();
        after.Version.ShouldBeNull();
        after.TotalMinutes.ShouldBe(0);
        after.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Removing_one_of_two_sessions_keeps_the_day()
    {
        var planBuilder = DietPlanBuilder.APlan().StartedDaysAgo(30);
        var plan = planBuilder.Build();
        var userId = planBuilder.UserId;

        var day = ExerciseDayBuilder.ADay()
            .ForUser(userId).ForPlan(plan.Id)
            .Did(FakeActivityTypeRepository.Running(), 45)
            .Did(FakeActivityTypeRepository.BriskWalk(), 30)
            .Build();

        var days = FakeExerciseDayRepository.Containing(day);

        var delete = new DietApi.Features.Exercise.DeleteExerciseEntry.DeleteExerciseEntryHandler(
            days, SignedInUser.WithId(userId));

        var result = await delete.Handle(
            new DietApi.Features.Exercise.DeleteExerciseEntry.DeleteExerciseEntryCommand(
                day.EntriesInOrder().First().Id, day.Version),
            CancellationToken.None);

        result.Found.ShouldBeTrue();
        result.Day.ShouldNotBeNull();
        result.Day.TotalMinutes.ShouldBe(30);
        days.DeleteCount.ShouldBe(0);
        days.Stored.Count.ShouldBe(1);
    }
}
