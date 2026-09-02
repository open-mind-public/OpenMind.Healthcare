using DayRange = DietApi.Features.FoodLog.DayRangeResponse;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietPlan;
using DietApi.Features.DietPlan.SetTargets;
using DietApi.Features.FoodLog.GetDay;
using DietApi.Features.FoodLog.GetDayRange;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Features;

/// <summary>
/// Lowering a target must not reach backwards and turn a day the member already saw as a success
/// into a failure.
/// </summary>
/// <remarks>
/// This is the single most valuable regression test in the feature. Without the per-day target
/// snapshot, a member who tightens their goal would watch weeks of green days turn red, and no
/// amount of explaining would make that feel like anything other than the app breaking.
/// </remarks>
public class TargetSnapshotTests
{
    [Fact]
    public async Task Lowering_the_plan_target_does_not_flip_days_already_assessed()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(10).WithTargets(2100);
        var plan = builder.Build();
        var planRepo = FakeDietPlanRepository.Containing(plan);

        // A day logged at 1,824 calories against the 2,100 target then in force: comfortably on target.
        var day = LoggedDayBuilder.ADay()
            .ForUser(builder.UserId).ForPlan(plan.Id)
            .PlanStartedDaysAgo(10).DaysAgo(1)
            .Targeting(2100)
            .Ate(FakeFoodLibraryRepository.Oats(), quantity: 8m)
            .Build();

        var dayRepo = FakeLoggedDayRepository.Containing(day);

        var beforeRange = await Range(planRepo, dayRepo, builder);
        beforeRange.Days.Single(d => d.Date == builder.DaysAgo(1)).State.ShouldBe(DayState.OnTarget);

        // The member now tightens their goal to 1,500 - below what that day consumed.
        var setTargets = new SetDietTargetsHandler(planRepo, SignedInUser.WithId(builder.UserId));
        await setTargets.Handle(
            new SetDietTargetsCommand(new SetTargetsRequest(
                new NutritionTargetsDto(1500, null, null, null), TargetSource.MemberSet)),
            CancellationToken.None);

        plan.Targets.Calories.ShouldBe(1500);

        // The already-assessed day is untouched.
        var afterRange = await Range(planRepo, dayRepo, builder);
        afterRange.Days.Single(d => d.Date == builder.DaysAgo(1)).State.ShouldBe(DayState.OnTarget);
        afterRange.Days.Single(d => d.Date == builder.DaysAgo(1)).TargetCalories.ShouldBe(2100);
    }

    [Fact]
    public async Task A_day_read_back_in_detail_also_keeps_its_original_target()
    {
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(10).WithTargets(2100);
        var plan = builder.Build();
        var planRepo = FakeDietPlanRepository.Containing(plan);

        var day = LoggedDayBuilder.ADay()
            .ForUser(builder.UserId).ForPlan(plan.Id)
            .PlanStartedDaysAgo(10).DaysAgo(1)
            .Targeting(2100)
            .Ate(FakeFoodLibraryRepository.Oats(), quantity: 8m)
            .Build();

        var dayRepo = FakeLoggedDayRepository.Containing(day);

        var setTargets = new SetDietTargetsHandler(planRepo, SignedInUser.WithId(builder.UserId));
        await setTargets.Handle(
            new SetDietTargetsCommand(new SetTargetsRequest(
                new NutritionTargetsDto(1500, null, null, null), TargetSource.MemberSet)),
            CancellationToken.None);

        var getDay = new GetDayHandler(planRepo, dayRepo, SignedInUser.WithId(builder.UserId));
        var result = await getDay.Handle(new GetDayQuery(builder.DaysAgo(1)), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Targets.Calories.ShouldBe(2100);
        result.State.ShouldBe(DayState.OnTarget);
    }

    [Fact]
    public async Task A_day_logged_after_the_change_uses_the_new_target()
    {
        // The snapshot protects the past. It must not freeze the future.
        var builder = DietPlanBuilder.APlan().StartedDaysAgo(10).WithTargets(2100);
        var plan = builder.Build();
        var planRepo = FakeDietPlanRepository.Containing(plan);

        var setTargets = new SetDietTargetsHandler(planRepo, SignedInUser.WithId(builder.UserId));
        await setTargets.Handle(
            new SetDietTargetsCommand(new SetTargetsRequest(
                new NutritionTargetsDto(1500, null, null, null), TargetSource.MemberSet)),
            CancellationToken.None);

        var getDay = new GetDayHandler(planRepo, FakeLoggedDayRepository.Empty(), SignedInUser.WithId(builder.UserId));
        var today = await getDay.Handle(new GetDayQuery(builder.Today), CancellationToken.None);

        today.ShouldNotBeNull();
        today.Targets.Calories.ShouldBe(1500);
    }

    private static async Task<DayRange> Range(
        FakeDietPlanRepository planRepo, FakeLoggedDayRepository dayRepo, DietPlanBuilder builder)
    {
        var handler = new GetDayRangeHandler(planRepo, dayRepo, SignedInUser.WithId(builder.UserId));
        var range = await handler.Handle(
            new GetDayRangeQuery(builder.DaysAgo(5), builder.Today), CancellationToken.None);
        return range!;
    }
}
