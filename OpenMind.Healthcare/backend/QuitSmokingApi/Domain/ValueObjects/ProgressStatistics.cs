using DDD.BuildingBlocks;

namespace QuitSmokingApi.Domain.ValueObjects;

/// <summary>
/// Value object representing progress statistics - read-only snapshot of journey progress.
/// Value objects are immutable and identified by their values, not by an ID.
/// Days the user marked as smoked are excluded from every smoke-free total.
/// </summary>
public class ProgressStatistics(
    int daysSmokeFree,
    int hoursSmokeFree,
    int minutesSmokeFree,
    int cigarettesAvoided,
    Money moneySaved,
    Duration lifeRegained,
    double progressPercentage,
    Milestone currentMilestone,
    Milestone? nextMilestone,
    int daysToNextMilestone,
    int totalDaysInJourney = 0,
    int smokedDays = 0,
    int cigarettesSmoked = 0,
    Money? moneySpentOnRelapses = null,
    int currentStreak = 0,
    int longestStreak = 0)
    : ValueObject
{
    public int DaysSmokeFree { get; } = daysSmokeFree;
    public int HoursSmokeFree { get; } = hoursSmokeFree;
    public int MinutesSmokeFree { get; } = minutesSmokeFree;
    public int CigarettesAvoided { get; } = cigarettesAvoided;
    public Money MoneySaved { get; } = moneySaved;
    public Duration LifeRegained { get; } = lifeRegained;
    public double ProgressPercentage { get; } = Math.Round(progressPercentage, 2);
    public Milestone CurrentMilestone { get; } = currentMilestone;
    public Milestone? NextMilestone { get; } = nextMilestone;
    public int DaysToNextMilestone { get; } = daysToNextMilestone;

    /// <summary>Calendar days elapsed since the quit date, smoked days included.</summary>
    public int TotalDaysInJourney { get; } = totalDaysInJourney;

    /// <summary>Days the user marked as smoked - excluded from <see cref="DaysSmokeFree"/>.</summary>
    public int SmokedDays { get; } = smokedDays;

    public int CigarettesSmoked { get; } = cigarettesSmoked;
    public Money MoneySpentOnRelapses { get; } = moneySpentOnRelapses ?? Money.Zero(moneySaved.Currency);

    /// <summary>Consecutive smoke-free days up to today.</summary>
    public int CurrentStreak { get; } = currentStreak;

    /// <summary>Longest run of consecutive smoke-free days in the journey.</summary>
    public int LongestStreak { get; } = longestStreak;

    public double SmokeFreeRate { get; } = RelapseAnalytics.Percentage(daysSmokeFree, totalDaysInJourney);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DaysSmokeFree;
        yield return CigarettesAvoided;
        yield return MoneySaved;
        yield return SmokedDays;
    }
}
