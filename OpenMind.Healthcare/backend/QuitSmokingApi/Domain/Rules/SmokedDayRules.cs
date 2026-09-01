using DDD.BuildingBlocks;

namespace QuitSmokingApi.Domain.Rules;

/// <summary>
/// Business rule: a day cannot be marked as smoked before the journey started
/// </summary>
public class SmokedDayCannotBeBeforeQuitDateRule(DateOnly date, DateOnly quitDate) : IBusinessRule
{
    public string RuleName => nameof(SmokedDayCannotBeBeforeQuitDateRule);

    public string ErrorMessage => $"A smoked day cannot be earlier than the quit date ({quitDate:yyyy-MM-dd})";

    public bool IsBroken() => date < quitDate;
}

/// <summary>
/// Business rule: a day cannot be marked as smoked in the future
/// </summary>
public class SmokedDayCannotBeInFutureRule(DateOnly date, DateOnly today) : IBusinessRule
{
    public string RuleName => nameof(SmokedDayCannotBeInFutureRule);

    public string ErrorMessage => "A smoked day cannot be in the future";

    public bool IsBroken() => date > today;
}

/// <summary>
/// Business rule: marking a day as smoked means at least one cigarette was smoked
/// </summary>
public class CigarettesSmokedMustBePositiveRule(int cigarettesSmoked) : IBusinessRule
{
    public string RuleName => nameof(CigarettesSmokedMustBePositiveRule);

    public string ErrorMessage => "Cigarettes smoked must be at least one";

    public bool IsBroken() => cigarettesSmoked <= 0;
}
