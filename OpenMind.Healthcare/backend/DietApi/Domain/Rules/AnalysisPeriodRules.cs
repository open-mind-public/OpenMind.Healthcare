using DDD.BuildingBlocks;

namespace DietApi.Domain.Rules;

/// <summary>
/// Business rule: an analysis period cannot reach outside the member's plan or into the future.
/// </summary>
/// <remarks>
/// Days before the plan started and days after today are neither successes nor misses - counting
/// them would make a member who joined on Thursday look like they had a quiet quarter (FR-002).
/// </remarks>
public class PeriodMustFallWithinPlanRule(DateOnly from, DateOnly to, DateOnly planStartDate, DateOnly today)
    : IBusinessRule
{
    public string RuleName => nameof(PeriodMustFallWithinPlanRule);

    public string ErrorMessage =>
        $"An analysis period must fall between your plan start ({planStartDate:yyyy-MM-dd}) and today";

    public bool IsBroken() => from < planStartDate || to > today;
}

/// <summary>
/// Business rule: an analysis period must contain at least one day.
/// </summary>
public class PeriodMustNotBeEmptyRule(DateOnly from, DateOnly to) : IBusinessRule
{
    public string RuleName => nameof(PeriodMustNotBeEmptyRule);

    public string ErrorMessage => "An analysis period must cover at least one day";

    public bool IsBroken() => to < from;
}
