using DDD.BuildingBlocks;

namespace DietApi.Domain.Rules;

/// <summary>
/// Business rule: exercise cannot be recorded for a future date.
/// </summary>
public class ExerciseDateCannotBeInFutureRule(DateOnly date, DateOnly today) : IBusinessRule
{
    public string RuleName => nameof(ExerciseDateCannotBeInFutureRule);

    public string ErrorMessage => "You cannot record exercise for a future date";

    public bool IsBroken() => date > today;
}

/// <summary>
/// Business rule: exercise cannot predate the plan it belongs to.
/// </summary>
public class ExerciseDateCannotPrecedePlanStartRule(DateOnly date, DateOnly planStartDate) : IBusinessRule
{
    public string RuleName => nameof(ExerciseDateCannotPrecedePlanStartRule);

    public string ErrorMessage => $"That date is before your plan started on {planStartDate:yyyy-MM-dd}";

    public bool IsBroken() => date < planStartDate;
}

/// <summary>
/// Business rule: a session must have lasted some whole number of minutes.
/// </summary>
public class DurationMustBePositiveRule(int durationMinutes) : IBusinessRule
{
    public string RuleName => nameof(DurationMustBePositiveRule);

    public string ErrorMessage => "How long did it last? Enter a duration of at least one minute";

    public bool IsBroken() => durationMinutes <= 0;
}

/// <summary>
/// Business rule: a single session cannot claim more time than a day contains.
/// </summary>
/// <remarks>
/// The ceiling is a day, not an athletic judgement: ultra-endurance sessions are real, and the
/// rule exists to catch a member typing hours into a minutes field, not to tell them their
/// training was implausible.
/// </remarks>
public class DurationWithinCeilingRule(int durationMinutes) : IBusinessRule
{
    public const int CeilingMinutes = 1_440;

    public string RuleName => nameof(DurationWithinCeilingRule);

    public string ErrorMessage =>
        $"A single session cannot be longer than {CeilingMinutes:N0} minutes - check you entered minutes, not hours";

    public bool IsBroken() => durationMinutes > CeilingMinutes;
}
