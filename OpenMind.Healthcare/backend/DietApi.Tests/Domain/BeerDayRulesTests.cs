using DDD.BuildingBlocks;
using DietApi.Domain.Aggregates;

namespace DietApi.Tests.Domain;

/// <summary>
/// The two date rules on marking a beer day: not in the future, not before the plan started.
/// </summary>
public class BeerDayRulesTests
{
    private static readonly DateTime Clock = DateTime.UtcNow;
    private static readonly DateOnly Today = DateOnly.FromDateTime(Clock);
    private static readonly DateOnly PlanStart = Today.AddDays(-30);
    private static readonly Guid Plan = Guid.NewGuid();
    private static readonly Guid User = Guid.NewGuid();

    [Fact]
    public void A_past_date_within_the_plan_can_be_marked()
    {
        var day = BeerDay.Mark(Plan, User, Today.AddDays(-3), PlanStart, Clock);

        day.Date.ShouldBe(Today.AddDays(-3));
        day.UserId.ShouldBe(User);
        day.DietPlanId.ShouldBe(Plan);
    }

    [Fact]
    public void Today_can_be_marked()
    {
        Should.NotThrow(() => BeerDay.Mark(Plan, User, Today, PlanStart, Clock));
    }

    [Fact]
    public void A_future_date_is_refused()
    {
        var ex = Should.Throw<BusinessRuleValidationException>(
            () => BeerDay.Mark(Plan, User, Today.AddDays(1), PlanStart, Clock));

        ex.RuleName.ShouldBe("BeerDateCannotBeInFutureRule");
    }

    [Fact]
    public void A_date_before_the_plan_started_is_refused()
    {
        var ex = Should.Throw<BusinessRuleValidationException>(
            () => BeerDay.Mark(Plan, User, PlanStart.AddDays(-1), PlanStart, Clock));

        ex.RuleName.ShouldBe("BeerDateCannotPrecedePlanStartRule");
    }

    [Fact]
    public void Marking_emits_an_event_carrying_only_the_date()
    {
        var day = BeerDay.Mark(Plan, User, Today.AddDays(-2), PlanStart, Clock);

        var evt = day.DomainEvents.ShouldHaveSingleItem();
        evt.ShouldBeOfType<DietApi.Domain.Events.BeerDayMarkedEvent>()
            .Date.ShouldBe(Today.AddDays(-2));
    }
}
