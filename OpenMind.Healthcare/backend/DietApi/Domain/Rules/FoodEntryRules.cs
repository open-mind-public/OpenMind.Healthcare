using DDD.BuildingBlocks;

namespace DietApi.Domain.Rules;

/// <summary>
/// Business rule: a food entry cannot be dated in the future.
/// </summary>
public class EntryDateCannotBeInFutureRule(DateOnly date, DateOnly today) : IBusinessRule
{
    public string RuleName => nameof(EntryDateCannotBeInFutureRule);

    public string ErrorMessage => "You cannot log food for a future date";

    public bool IsBroken() => date > today;
}

/// <summary>
/// Business rule: a food entry cannot predate the plan it belongs to.
/// </summary>
public class EntryDateCannotPrecedePlanStartRule(DateOnly date, DateOnly planStartDate) : IBusinessRule
{
    public string RuleName => nameof(EntryDateCannotPrecedePlanStartRule);

    public string ErrorMessage => $"That date is before your plan started on {planStartDate:yyyy-MM-dd}";

    public bool IsBroken() => date < planStartDate;
}

/// <summary>
/// Business rule: a quantity must be positive. Fractional quantities are fine.
/// </summary>
public class QuantityMustBePositiveRule(decimal quantity) : IBusinessRule
{
    public string RuleName => nameof(QuantityMustBePositiveRule);

    public string ErrorMessage => "Quantity must be greater than zero";

    public bool IsBroken() => quantity <= 0;
}

/// <summary>
/// Business rule: a single entry cannot claim an implausible calorie count.
/// </summary>
/// <remarks>
/// Without a ceiling one mistyped quantity silently distorts every statistic that follows.
/// </remarks>
public class EntryCaloriesWithinCeilingRule(int calories) : IBusinessRule
{
    public const int CeilingKcal = 10_000;

    public string RuleName => nameof(EntryCaloriesWithinCeilingRule);

    public string ErrorMessage => $"A single entry cannot exceed {CeilingKcal:N0} calories - check the quantity";

    public bool IsBroken() => calories > CeilingKcal;
}
