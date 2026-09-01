using QuitSmokingApi.Domain.Entities;
using QuitSmokingApi.Domain.ValueObjects;

namespace QuitSmokingApi.Features.SmokedDays;

/// <summary>
/// DTO for a single day the user marked as smoked (a "failed" day)
/// </summary>
public record SmokedDayDto(
    Guid Id,
    DateOnly Date,
    int CigarettesSmoked,
    string Trigger,
    string? Note,
    decimal MoneySpent,
    string Currency,
    DateTime RecordedAt
);

/// <summary>
/// Request body used to mark (or amend) a day as smoked
/// </summary>
public record MarkSmokedDayRequest(
    DateOnly Date,
    int CigarettesSmoked = 1,
    RelapseTrigger Trigger = RelapseTrigger.Unspecified,
    string? Note = null
);

/// <summary>
/// DTO for the relapse analytics snapshot
/// </summary>
public record RelapseAnalyticsDto(
    int TotalDaysInJourney,
    int SmokeFreeDays,
    int SmokedDays,
    double SmokeFreeRate,
    double RelapseRate,
    int TotalCigarettesSmoked,
    decimal MoneySpentOnRelapses,
    decimal MoneySaved,
    string Currency,
    int LifeLostMinutes,
    string LifeLostFormatted,
    int CurrentStreak,
    int LongestStreak,
    DateOnly? LastRelapseDate,
    DateOnly? FirstRelapseDate,
    int DaysSinceLastRelapse,
    double AverageCigarettesPerRelapseDay,
    double AverageDaysBetweenRelapses,
    int RelapsesLast30Days,
    int RelapsesPrevious30Days,
    string Trend,
    string? MostCommonTrigger,
    string? RiskiestWeekday,
    IReadOnlyList<TriggerStatDto> TriggerBreakdown,
    IReadOnlyList<WeekdayStatDto> WeekdayBreakdown,
    IReadOnlyList<MonthlyStatDto> MonthlyBreakdown
);

public record TriggerStatDto(string Trigger, int Days, int Cigarettes, double SharePercentage);

public record WeekdayStatDto(string Weekday, int SmokedDays, int TotalDays, double RelapseRate);

public record MonthlyStatDto(int Year, int Month, string Label, int SmokedDays, int SmokeFreeDays, int TotalDays, int Cigarettes, double SmokeFreeRate);

/// <summary>
/// Maps smoked-day domain objects onto their transport representations.
/// </summary>
public static class SmokedDayMapper
{
    public static SmokedDayDto ToDto(SmokedDay day, Money pricePerCigarette) => new(
        Id: day.Id,
        Date: day.Date,
        CigarettesSmoked: day.CigarettesSmoked,
        Trigger: day.Trigger.ToString(),
        Note: day.Note,
        MoneySpent: pricePerCigarette.Multiply(day.CigarettesSmoked).Amount,
        Currency: pricePerCigarette.Currency,
        RecordedAt: day.RecordedAt
    );

    public static RelapseAnalyticsDto ToDto(RelapseAnalytics analytics) => new(
        TotalDaysInJourney: analytics.TotalDaysInJourney,
        SmokeFreeDays: analytics.SmokeFreeDays,
        SmokedDays: analytics.SmokedDays,
        SmokeFreeRate: analytics.SmokeFreeRate,
        RelapseRate: analytics.RelapseRate,
        TotalCigarettesSmoked: analytics.TotalCigarettesSmoked,
        MoneySpentOnRelapses: analytics.MoneySpentOnRelapses.Amount,
        MoneySaved: analytics.MoneySaved.Amount,
        Currency: analytics.MoneySpentOnRelapses.Currency,
        LifeLostMinutes: analytics.LifeLostToRelapses.TotalMinutes,
        LifeLostFormatted: analytics.LifeLostToRelapses.ToFriendlyString(),
        CurrentStreak: analytics.CurrentStreak,
        LongestStreak: analytics.LongestStreak,
        LastRelapseDate: analytics.LastRelapseDate,
        FirstRelapseDate: analytics.FirstRelapseDate,
        DaysSinceLastRelapse: analytics.DaysSinceLastRelapse,
        AverageCigarettesPerRelapseDay: analytics.AverageCigarettesPerRelapseDay,
        AverageDaysBetweenRelapses: analytics.AverageDaysBetweenRelapses,
        RelapsesLast30Days: analytics.RelapsesLast30Days,
        RelapsesPrevious30Days: analytics.RelapsesPrevious30Days,
        Trend: analytics.Trend.ToString(),
        MostCommonTrigger: analytics.MostCommonTrigger?.ToString(),
        RiskiestWeekday: analytics.RiskiestWeekday?.ToString(),
        TriggerBreakdown: analytics.TriggerBreakdown
            .Select(t => new TriggerStatDto(t.Trigger.ToString(), t.Days, t.Cigarettes, t.SharePercentage))
            .ToList(),
        WeekdayBreakdown: analytics.WeekdayBreakdown
            .Select(w => new WeekdayStatDto(w.Name, w.SmokedDays, w.TotalDays, w.RelapseRate))
            .ToList(),
        MonthlyBreakdown: analytics.MonthlyBreakdown
            .Select(m => new MonthlyStatDto(m.Year, m.Month, m.Label, m.SmokedDays, m.SmokeFreeDays, m.TotalDays, m.Cigarettes, m.SmokeFreeRate))
            .ToList()
    );
}
