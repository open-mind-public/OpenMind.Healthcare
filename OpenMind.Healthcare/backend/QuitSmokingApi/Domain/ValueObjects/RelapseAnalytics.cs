using System.Globalization;
using DDD.BuildingBlocks;
using QuitSmokingApi.Domain.Entities;

namespace QuitSmokingApi.Domain.ValueObjects;

/// <summary>
/// Value object holding a read-only analytics snapshot of the days the user smoked
/// ("failed" days) during the quit journey.
/// </summary>
public class RelapseAnalytics(
    int totalDaysInJourney,
    int smokeFreeDays,
    int smokedDays,
    int totalCigarettesSmoked,
    Money moneySpentOnRelapses,
    Money moneySaved,
    Duration lifeLostToRelapses,
    int currentStreak,
    int longestStreak,
    DateOnly? lastRelapseDate,
    DateOnly? firstRelapseDate,
    int daysSinceLastRelapse,
    double averageCigarettesPerRelapseDay,
    double averageDaysBetweenRelapses,
    int relapsesLast30Days,
    int relapsesPrevious30Days,
    RelapseTrend trend,
    RelapseTrigger? mostCommonTrigger,
    DayOfWeek? riskiestWeekday,
    IReadOnlyList<TriggerStat> triggerBreakdown,
    IReadOnlyList<WeekdayStat> weekdayBreakdown,
    IReadOnlyList<MonthlyStat> monthlyBreakdown)
    : ValueObject
{
    public int TotalDaysInJourney { get; } = totalDaysInJourney;
    public int SmokeFreeDays { get; } = smokeFreeDays;
    public int SmokedDays { get; } = smokedDays;
    public double SmokeFreeRate { get; } = Percentage(smokeFreeDays, totalDaysInJourney);
    public double RelapseRate { get; } = Percentage(smokedDays, totalDaysInJourney);
    public int TotalCigarettesSmoked { get; } = totalCigarettesSmoked;
    public Money MoneySpentOnRelapses { get; } = moneySpentOnRelapses;
    public Money MoneySaved { get; } = moneySaved;
    public Duration LifeLostToRelapses { get; } = lifeLostToRelapses;
    public int CurrentStreak { get; } = currentStreak;
    public int LongestStreak { get; } = longestStreak;
    public DateOnly? LastRelapseDate { get; } = lastRelapseDate;
    public DateOnly? FirstRelapseDate { get; } = firstRelapseDate;
    public int DaysSinceLastRelapse { get; } = daysSinceLastRelapse;
    public double AverageCigarettesPerRelapseDay { get; } = Math.Round(averageCigarettesPerRelapseDay, 1);
    public double AverageDaysBetweenRelapses { get; } = Math.Round(averageDaysBetweenRelapses, 1);
    public int RelapsesLast30Days { get; } = relapsesLast30Days;
    public int RelapsesPrevious30Days { get; } = relapsesPrevious30Days;
    public RelapseTrend Trend { get; } = trend;
    public RelapseTrigger? MostCommonTrigger { get; } = mostCommonTrigger;
    public DayOfWeek? RiskiestWeekday { get; } = riskiestWeekday;
    public IReadOnlyList<TriggerStat> TriggerBreakdown { get; } = triggerBreakdown;
    public IReadOnlyList<WeekdayStat> WeekdayBreakdown { get; } = weekdayBreakdown;
    public IReadOnlyList<MonthlyStat> MonthlyBreakdown { get; } = monthlyBreakdown;

    public static RelapseAnalytics Empty(string currency = "USD") => new(
        totalDaysInJourney: 0,
        smokeFreeDays: 0,
        smokedDays: 0,
        totalCigarettesSmoked: 0,
        moneySpentOnRelapses: Money.Zero(currency),
        moneySaved: Money.Zero(currency),
        lifeLostToRelapses: Duration.Zero,
        currentStreak: 0,
        longestStreak: 0,
        lastRelapseDate: null,
        firstRelapseDate: null,
        daysSinceLastRelapse: 0,
        averageCigarettesPerRelapseDay: 0,
        averageDaysBetweenRelapses: 0,
        relapsesLast30Days: 0,
        relapsesPrevious30Days: 0,
        trend: RelapseTrend.NotEnoughData,
        mostCommonTrigger: null,
        riskiestWeekday: null,
        triggerBreakdown: [],
        weekdayBreakdown: [],
        monthlyBreakdown: []);

    internal static double Percentage(int part, int total) =>
        total <= 0 ? 0 : Math.Round((double)part / total * 100, 2);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TotalDaysInJourney;
        yield return SmokedDays;
        yield return TotalCigarettesSmoked;
        yield return CurrentStreak;
    }
}

/// <summary>
/// Direction the relapse frequency is heading in, comparing the last 30 days to the 30 before them.
/// </summary>
public enum RelapseTrend
{
    NotEnoughData = 0,
    Improving,
    Stable,
    Worsening
}

/// <summary>
/// How often a given trigger caused a relapse
/// </summary>
public class TriggerStat(RelapseTrigger trigger, int days, int cigarettes, int totalSmokedDays) : ValueObject
{
    public RelapseTrigger Trigger { get; } = trigger;
    public int Days { get; } = days;
    public int Cigarettes { get; } = cigarettes;
    public double SharePercentage { get; } = RelapseAnalytics.Percentage(days, totalSmokedDays);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Trigger;
        yield return Days;
        yield return Cigarettes;
    }
}

/// <summary>
/// Relapse frequency for one day of the week - reveals the user's riskiest weekday
/// </summary>
public class WeekdayStat(DayOfWeek weekday, int smokedDays, int totalDays) : ValueObject
{
    public DayOfWeek Weekday { get; } = weekday;
    public string Name { get; } = CultureInfo.InvariantCulture.DateTimeFormat.GetDayName(weekday);
    public int SmokedDays { get; } = smokedDays;
    public int TotalDays { get; } = totalDays;
    public double RelapseRate { get; } = RelapseAnalytics.Percentage(smokedDays, totalDays);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Weekday;
        yield return SmokedDays;
        yield return TotalDays;
    }
}

/// <summary>
/// Smoke-free versus smoked days for a single calendar month of the journey
/// </summary>
public class MonthlyStat(int year, int month, int smokedDays, int totalDays, int cigarettes) : ValueObject
{
    public int Year { get; } = year;
    public int Month { get; } = month;
    public string Label { get; } = $"{CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month)} {year}";
    public int SmokedDays { get; } = smokedDays;
    public int SmokeFreeDays { get; } = totalDays - smokedDays;
    public int TotalDays { get; } = totalDays;
    public int Cigarettes { get; } = cigarettes;
    public double SmokeFreeRate { get; } = RelapseAnalytics.Percentage(totalDays - smokedDays, totalDays);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Year;
        yield return Month;
        yield return SmokedDays;
        yield return TotalDays;
    }
}
