using DDD.BuildingBlocks;
using DietApi.Domain.Aggregates;
using DietApi.Domain.Rules;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// The four rules guarding a recorded session. Each is proven both to throw when broken and to
/// pass at its boundary - a rule that also refuses the legitimate edge is a bug, not caution.
/// </summary>
public class ExerciseEntryRulesTests
{
    [Fact]
    public void A_day_cannot_be_started_in_the_future()
    {
        var builder = ExerciseDayBuilder.ADay();

        var act = () =>
        {
            ExerciseDay.StartDay(
                builder.PlanId, builder.UserId, builder.Today.AddDays(1), builder.PlanStartDate, builder.Clock);
        };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(ExerciseDateCannotBeInFutureRule));
    }

    [Fact]
    public void Today_is_not_in_the_future()
    {
        var builder = ExerciseDayBuilder.ADay();

        var day = ExerciseDay.StartDay(
            builder.PlanId, builder.UserId, builder.Today, builder.PlanStartDate, builder.Clock);

        day.Date.ShouldBe(builder.Today);
    }

    [Fact]
    public void A_day_cannot_predate_the_plan_it_belongs_to()
    {
        var builder = ExerciseDayBuilder.ADay().PlanStartedDaysAgo(10);

        var act = () =>
        {
            ExerciseDay.StartDay(
                builder.PlanId, builder.UserId, builder.PlanStartDate.AddDays(-1), builder.PlanStartDate, builder.Clock);
        };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(ExerciseDateCannotPrecedePlanStartRule));
    }

    [Fact]
    public void The_plan_start_date_itself_is_allowed()
    {
        var builder = ExerciseDayBuilder.ADay().PlanStartedDaysAgo(10);

        var day = ExerciseDay.StartDay(
            builder.PlanId, builder.UserId, builder.PlanStartDate, builder.PlanStartDate, builder.Clock);

        day.Date.ShouldBe(builder.PlanStartDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-45)]
    public void A_session_must_have_lasted_some_time(int durationMinutes)
    {
        var builder = ExerciseDayBuilder.ADay();
        var day = builder.Build();
        var running = FakeActivityTypeRepository.Running();

        var act = () =>
        {
            day.AddEntry(running.Id, running.Name, running.Met, durationMinutes, builder.WeightKg, builder.Clock);
        };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(DurationMustBePositiveRule));
    }

    [Fact]
    public void One_minute_is_a_session()
    {
        var builder = ExerciseDayBuilder.ADay();
        var day = builder.Build();
        var walk = FakeActivityTypeRepository.BriskWalk();

        day.AddEntry(walk.Id, walk.Name, walk.Met, 1, builder.WeightKg, builder.Clock);

        day.Totals.Minutes.ShouldBe(1);
    }

    [Fact]
    public void A_session_cannot_be_longer_than_a_day()
    {
        var builder = ExerciseDayBuilder.ADay();
        var day = builder.Build();
        var running = FakeActivityTypeRepository.Running();

        var act = () =>
        {
            day.AddEntry(
                running.Id, running.Name, running.Met,
                DurationWithinCeilingRule.CeilingMinutes + 1, builder.WeightKg, builder.Clock);
        };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(DurationWithinCeilingRule));
    }

    [Fact]
    public void Exactly_a_full_day_is_allowed()
    {
        var builder = ExerciseDayBuilder.ADay();
        var day = builder.Build();
        var walk = FakeActivityTypeRepository.BriskWalk();

        day.AddEntry(
            walk.Id, walk.Name, walk.Met,
            DurationWithinCeilingRule.CeilingMinutes, builder.WeightKg, builder.Clock);

        day.Totals.Minutes.ShouldBe(DurationWithinCeilingRule.CeilingMinutes);
    }

    [Fact]
    public void The_same_rules_guard_an_edit()
    {
        var builder = ExerciseDayBuilder.ADay();
        var running = FakeActivityTypeRepository.Running();
        var day = builder.Did(running, 45).Build();
        var entryId = day.Entries.Single().Id;

        var act = () =>
        {
            day.UpdateEntry(entryId, running.Id, running.Name, running.Met, 0, builder.WeightKg);
        };

        act.ShouldThrow<BusinessRuleValidationException>()
            .RuleName.ShouldBe(nameof(DurationMustBePositiveRule));
    }
}
