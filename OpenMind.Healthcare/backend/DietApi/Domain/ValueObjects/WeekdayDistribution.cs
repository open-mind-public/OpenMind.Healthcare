using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

public record WeekdayShare(DayOfWeek DayOfWeek, int AverageKilocalories, int LoggedDays);

/// <summary>
/// Intake across the days of the week.
/// </summary>
/// <remarks>
/// Built from each logged day's calendar date, which is a <c>DateOnly</c> and carries no timezone
/// component at all. Only the time-of-day distribution has that problem; this one does not.
/// <para>
/// Each weekday's figure is an <em>average</em> over the days of that weekday actually logged, and
/// carries the count. A member with four Mondays and one Sunday would otherwise appear to eat four
/// times as much on Mondays.
/// </para>
/// </remarks>
public class WeekdayDistribution : ValueObject
{
    public IReadOnlyList<WeekdayShare> Shares { get; private set; } = [];

    private WeekdayDistribution() { }

    public static WeekdayDistribution Create(IReadOnlyList<WeekdayShare> shares)
    {
        if (shares.Count != 7 || shares.Select(s => s.DayOfWeek).Distinct().Count() != 7)
            throw new DomainException("A weekday distribution must cover every day of the week exactly once");

        // Monday first, matching the calendar the rest of the programme draws.
        return new WeekdayDistribution
        {
            Shares = [.. shares.OrderBy(s => ((int)s.DayOfWeek + 6) % 7)]
        };
    }

    /// <summary>Average daily energy across Saturday and Sunday, or null when neither was logged.</summary>
    public int? WeekendAverage => AverageOver([DayOfWeek.Saturday, DayOfWeek.Sunday]);

    /// <summary>Average daily energy across Monday to Friday, or null when none were logged.</summary>
    public int? WeekdayAverage =>
        AverageOver([DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]);

    public int LoggedDaysOn(params DayOfWeek[] days) =>
        Shares.Where(s => days.Contains(s.DayOfWeek)).Sum(s => s.LoggedDays);

    /// <summary>
    /// Weighted by how many days of each weekday were logged, so two heavy Saturdays and one light
    /// Sunday give the honest weekend average rather than the average of two averages.
    /// </summary>
    private int? AverageOver(DayOfWeek[] days)
    {
        var relevant = Shares.Where(s => days.Contains(s.DayOfWeek) && s.LoggedDays > 0).ToList();

        if (relevant.Count == 0)
            return null;

        var totalEnergy = relevant.Sum(s => (long)s.AverageKilocalories * s.LoggedDays);
        var totalDays = relevant.Sum(s => s.LoggedDays);

        return (int)Math.Round((decimal)totalEnergy / totalDays, MidpointRounding.AwayFromZero);
    }

    protected override IEnumerable<object?> GetEqualityComponents() => Shares.Cast<object?>();
}
