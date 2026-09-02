namespace DietApi.Domain.ValueObjects;

/// <summary>
/// What an achievement measures. The threshold that goes with it is a day count in every case.
/// </summary>
public enum AchievementCriterion
{
    ConsecutiveOnTargetDays,
    TotalDaysLogged,
    DaysOnPlan
}
