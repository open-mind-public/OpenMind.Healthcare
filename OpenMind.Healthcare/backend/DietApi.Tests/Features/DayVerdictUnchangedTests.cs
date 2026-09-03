using LoggedDayAggregate = DietApi.Domain.Aggregates.LoggedDay;
using DietApi.Domain.ValueObjects;
using DietApi.Features.Exercise;
using DietApi.Features.Exercise.AddExerciseEntry;
using DietApi.Features.FoodLog;
using DietApi.Features.FoodLog.GetDay;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// The guarantee this whole feature is shaped around: recording exercise never moves a day's
/// eating verdict, in either direction, ever.
/// </summary>
/// <remarks>
/// <para>
/// It is the easiest thing in the feature to break by being helpful. Adding the estimate to the
/// day's allowance, nudging the target, marking an over-target day as forgiven because the member
/// ran - each is a small, well-meaning change, and each turns a calorie target into a number that
/// moves when you exercise. A member cannot trust a target that does that.
/// </para>
/// <para>
/// So this asserts the absence directly: the target snapshot, the consumed total and the day
/// state are all exactly what they were, including when the exercise is recorded days after the
/// day it happened on. If a future change breaks this, it breaks here (FR-015, SC-008).
/// </para>
/// </remarks>
public class DayVerdictUnchangedTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task An_over_target_day_stays_over_target_when_exercise_is_recorded_days_later()
    {
        var (plan, planRepo, userId) = APlan();
        var fiveDaysAgo = Today.AddDays(-5);

        // Twelve bowls of porridge: 2,736 kcal against a 2,100 target.
        var loggedDay = LoggedDayBuilder.ADay()
            .ForUser(userId).ForPlan(plan.Id).DaysAgo(5).Targeting(2100)
            .Ate(FakeFoodLibraryRepository.Oats(), quantity: 12m)
            .Build();

        loggedDay.Assess().State.ShouldBe(DayState.OverTarget);

        var before = Snapshot(loggedDay);

        // A long, hard session against that same past date, recorded today.
        await RecordExercise(plan.Id, planRepo, userId, fiveDaysAgo, FakeActivityTypeRepository.Butterfly(), 120);

        AssertUnchanged(loggedDay, before);
        loggedDay.Assess().State.ShouldBe(DayState.OverTarget);
    }

    [Fact]
    public async Task An_on_target_day_stays_on_target()
    {
        var (plan, planRepo, userId) = APlan();
        var threeDaysAgo = Today.AddDays(-3);

        var loggedDay = LoggedDayBuilder.ADay()
            .ForUser(userId).ForPlan(plan.Id).DaysAgo(3).Targeting(2100)
            .Ate(FakeFoodLibraryRepository.Oats(), quantity: 5m)
            .Build();

        loggedDay.Assess().State.ShouldBe(DayState.OnTarget);

        var before = Snapshot(loggedDay);

        await RecordExercise(plan.Id, planRepo, userId, threeDaysAgo, FakeActivityTypeRepository.Running(), 60);

        AssertUnchanged(loggedDay, before);
        loggedDay.Assess().State.ShouldBe(DayState.OnTarget);

        // In particular: exercising does not turn an on-target day into anything better, and does
        // not create headroom that lets the member eat past the target and stay on it.
        loggedDay.Assess().RemainingCalories.ShouldBe(before.Remaining);
    }

    [Fact]
    public async Task A_day_with_exercise_and_no_food_is_still_not_logged_for_eating()
    {
        var (plan, planRepo, userId) = APlan();
        var yesterday = Today.AddDays(-1);

        await RecordExercise(plan.Id, planRepo, userId, yesterday, FakeActivityTypeRepository.Running(), 45);

        // The eating side has no day at all for that date, and says so.
        var foodHandler = new GetDayHandler(planRepo, FakeLoggedDayRepository.Empty(), SignedInUser.WithId(userId));
        var day = await foodHandler.Handle(new GetDayQuery(yesterday), CancellationToken.None);

        day.ShouldNotBeNull();
        day.State.ShouldBe(DayState.NotLogged);
        day.Totals.Calories.ShouldBe(0);
        day.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Recording_exercise_does_not_touch_the_plans_target_or_activity_level()
    {
        var (plan, planRepo, userId) = APlan();

        var targetBefore = plan.Targets.Calories;
        var sourceBefore = plan.TargetSource;
        var activityBefore = plan.ActivityLevel;

        await RecordExercise(plan.Id, planRepo, userId, Today, FakeActivityTypeRepository.Butterfly(), 180);

        plan.Targets.Calories.ShouldBe(targetBefore);
        plan.TargetSource.ShouldBe(sourceBefore);
        plan.ActivityLevel.ShouldBe(activityBefore);
    }

    [Fact]
    public async Task Deleting_the_exercise_leaves_the_eating_verdict_where_it_was_too()
    {
        // The mirror of the guarantee: if adding exercise cannot move the verdict, neither can
        // taking it away.
        var (plan, planRepo, userId) = APlan();

        var loggedDay = LoggedDayBuilder.ADay()
            .ForUser(userId).ForPlan(plan.Id).DaysAgo(2).Targeting(2100)
            .Ate(FakeFoodLibraryRepository.Oats(), quantity: 12m)
            .Build();

        var before = Snapshot(loggedDay);

        var days = FakeExerciseDayRepository.Empty();
        var running = FakeActivityTypeRepository.Running();
        var handler = new AddExerciseEntryHandler(
            planRepo, days, FakeActivityTypeRepository.Containing(running), SignedInUser.WithId(userId));

        await handler.Handle(
            new AddExerciseEntryCommand(Today.AddDays(-2), new AddExerciseEntryRequest(running.Id, 45, null)),
            CancellationToken.None);

        var exerciseDay = days.Stored.Single();
        exerciseDay.RemoveEntry(exerciseDay.Entries.Single().Id);

        AssertUnchanged(loggedDay, before);
    }

    [Fact]
    public void No_food_log_response_shape_carries_an_exercise_field()
    {
        // A structural guarantee, asserted rather than trusted. The eating contract knows nothing
        // about exercise, so a client cannot accidentally present a combined figure (SC-009).
        var eatingShapes = new[]
        {
            typeof(LoggedDayDto), typeof(FoodEntryDto), typeof(DaySummaryDto), typeof(DayRangeResponse)
        };

        foreach (var shape in eatingShapes)
        {
            foreach (var property in shape.GetProperties())
            {
                property.Name.ShouldNotContain("Exercise", Case.Insensitive);
                property.Name.ShouldNotContain("Activity", Case.Insensitive);
                property.Name.ShouldNotContain("Estimated", Case.Insensitive);
            }
        }
    }

    [Fact]
    public void No_exercise_response_shape_carries_a_target_or_a_day_state()
    {
        // And the reverse. Nothing on the exercise side offers a target, an allowance or a
        // verdict, so there is no field for a well-meaning client to add the estimate to (FR-016).
        var exerciseShapes = new[]
        {
            typeof(ExerciseDayDto), typeof(ExerciseEntryDto), typeof(ExerciseDaySummaryDto), typeof(ExerciseRangeResponse)
        };

        foreach (var shape in exerciseShapes)
        {
            foreach (var property in shape.GetProperties())
            {
                property.Name.ShouldNotContain("Target", Case.Insensitive);
                property.Name.ShouldNotContain("Remaining", Case.Insensitive);
                property.Name.ShouldNotContain("Available", Case.Insensitive);
                property.Name.ShouldNotContain("State", Case.Insensitive);
            }
        }
    }

    // --- Helpers ----------------------------------------------------------

    private record DayFacts(int TargetCalories, int ConsumedCalories, DayState State, int Remaining, int Overage, Guid Version);

    private static DayFacts Snapshot(LoggedDayAggregate day)
    {
        var assessment = day.Assess();

        return new DayFacts(
            day.TargetSnapshot.Calories,
            day.Totals.Calories,
            assessment.State,
            assessment.RemainingCalories,
            assessment.OverageCalories,
            day.Version);
    }

    private static void AssertUnchanged(LoggedDayAggregate day, DayFacts before)
    {
        Snapshot(day).ShouldBe(before);
    }

    private static async Task RecordExercise(
        Guid planId,
        FakeDietPlanRepository planRepo,
        Guid userId,
        DateOnly date,
        DietApi.Domain.Aggregates.ActivityType activity,
        int durationMinutes)
    {
        var handler = new AddExerciseEntryHandler(
            planRepo,
            FakeExerciseDayRepository.Empty(),
            FakeActivityTypeRepository.Containing(activity),
            SignedInUser.WithId(userId));

        var day = await handler.Handle(
            new AddExerciseEntryCommand(date, new AddExerciseEntryRequest(activity.Id, durationMinutes, null)),
            CancellationToken.None);

        day.ShouldNotBeNull();
        day.TotalKilocalories.ShouldBeGreaterThan(0);
        planId.ShouldNotBe(Guid.Empty);
    }

    private static (DietApi.Domain.Aggregates.DietPlan Plan, FakeDietPlanRepository Repo, Guid UserId) APlan()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(30).WithTargets(2100).Weighing(70m);
        var plan = builder.Build();
        return (plan, FakeDietPlanRepository.Containing(plan), builder.UserId);
    }
}
