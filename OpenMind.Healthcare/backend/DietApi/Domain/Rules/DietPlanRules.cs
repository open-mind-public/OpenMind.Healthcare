using DDD.BuildingBlocks;

namespace DietApi.Domain.Rules;

/// <summary>
/// Business rule: a plan cannot start in the future.
/// </summary>
/// <remarks>
/// The comparison instant is passed in rather than read from the clock, so the rule is testable
/// across arbitrary dates without freezing the system clock.
/// </remarks>
public class PlanStartDateCannotBeInFutureRule(DateOnly startDate, DateOnly today) : IBusinessRule
{
    public string RuleName => nameof(PlanStartDateCannotBeInFutureRule);

    public string ErrorMessage => "Plan start date cannot be in the future";

    public bool IsBroken() => startDate > today;
}

/// <summary>
/// Business rule: the daily calorie target must be positive.
/// </summary>
public class DailyCalorieTargetMustBePositiveRule(int calories) : IBusinessRule
{
    public string RuleName => nameof(DailyCalorieTargetMustBePositiveRule);

    public string ErrorMessage => "Daily calorie target must be greater than zero";

    public bool IsBroken() => calories <= 0;
}

/// <summary>
/// Business rule: height must be within a plausible human range.
/// </summary>
public class HeightMustBePlausibleRule(decimal heightCm) : IBusinessRule
{
    public const decimal MinimumCm = 50m;
    public const decimal MaximumCm = 250m;

    public string RuleName => nameof(HeightMustBePlausibleRule);

    public string ErrorMessage => $"Height must be between {MinimumCm:0} cm and {MaximumCm:0} cm";

    public bool IsBroken() => heightCm < MinimumCm || heightCm > MaximumCm;
}

/// <summary>
/// Business rule: age must be within a plausible range.
/// </summary>
public class AgeMustBePlausibleRule(int age) : IBusinessRule
{
    public const int Minimum = 13;
    public const int Maximum = 120;

    public string RuleName => nameof(AgeMustBePlausibleRule);

    public string ErrorMessage => $"Age must be between {Minimum} and {Maximum}";

    public bool IsBroken() => age < Minimum || age > Maximum;
}

/// <summary>
/// Business rule: a target weight, when set, must be within a plausible human range.
/// </summary>
public class TargetWeightMustBePlausibleRule(decimal? targetWeightKg) : IBusinessRule
{
    public const decimal MinimumKg = 20m;
    public const decimal MaximumKg = 500m;

    public string RuleName => nameof(TargetWeightMustBePlausibleRule);

    public string ErrorMessage => $"Target weight must be between {MinimumKg:0} kg and {MaximumKg:0} kg";

    public bool IsBroken() =>
        targetWeightKg is not null && (targetWeightKg < MinimumKg || targetWeightKg > MaximumKg);
}
