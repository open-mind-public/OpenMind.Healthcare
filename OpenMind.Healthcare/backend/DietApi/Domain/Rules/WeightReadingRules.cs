using DDD.BuildingBlocks;

namespace DietApi.Domain.Rules;

/// <summary>
/// Business rule: a weight reading cannot be dated in the future.
/// </summary>
public class WeightDateCannotBeInFutureRule(DateOnly date, DateOnly today) : IBusinessRule
{
    public string RuleName => nameof(WeightDateCannotBeInFutureRule);

    public string ErrorMessage => "Weight reading cannot be dated in the future";

    public bool IsBroken() => date > today;
}

/// <summary>
/// Business rule: a weight reading must be within a plausible human range.
/// </summary>
public class WeightMustBePlausibleRule(decimal weightKg) : IBusinessRule
{
    public const decimal MinimumKg = 20m;
    public const decimal MaximumKg = 500m;

    public string RuleName => nameof(WeightMustBePlausibleRule);

    public string ErrorMessage => $"Weight must be between {MinimumKg:0} kg and {MaximumKg:0} kg";

    public bool IsBroken() => weightKg < MinimumKg || weightKg > MaximumKg;
}

/// <summary>
/// Business rule: a plan cannot lose its only weight reading.
/// </summary>
/// <remarks>
/// The suggested daily target is computed from the member's current weight, and current weight
/// is the most recent reading. Deleting the last one would leave that calculation with no input,
/// so the deletion is refused and the member corrects the reading instead.
/// </remarks>
public class CannotRemoveLastWeightReadingRule(int readingCount) : IBusinessRule
{
    public string RuleName => nameof(CannotRemoveLastWeightReadingRule);

    public string ErrorMessage =>
        "This is your only weight reading. Edit it instead of deleting it - your daily target is calculated from it.";

    public bool IsBroken() => readingCount <= 1;
}
