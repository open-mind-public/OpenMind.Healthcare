using DDD.BuildingBlocks;

namespace DietApi.Domain.Rules;

/// <summary>
/// Business rule: a beer day cannot be recorded for a future date.
/// </summary>
public class BeerDateCannotBeInFutureRule(DateOnly date, DateOnly today) : IBusinessRule
{
    public string RuleName => nameof(BeerDateCannotBeInFutureRule);

    public string ErrorMessage => "You cannot mark a future date as a beer day";

    public bool IsBroken() => date > today;
}

/// <summary>
/// Business rule: a beer day cannot predate the plan it belongs to.
/// </summary>
public class BeerDateCannotPrecedePlanStartRule(DateOnly date, DateOnly planStartDate) : IBusinessRule
{
    public string RuleName => nameof(BeerDateCannotPrecedePlanStartRule);

    public string ErrorMessage => $"That date is before your plan started on {planStartDate:yyyy-MM-dd}";

    public bool IsBroken() => date < planStartDate;
}
