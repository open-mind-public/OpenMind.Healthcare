using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Observations.Rules;

/// <summary>
/// A large share of the day's energy logged late in the evening.
/// </summary>
/// <remarks>
/// Reviewed against FR-019: states a share and a time, and stops. It does not say late eating is
/// unhealthy, does not suggest eating earlier, and does not imply the member has a problem — all
/// of which are claims this programme is not qualified to make.
/// </remarks>
public class LateEatingRule : IObservationRule
{
    public const int LateHour = 21;
    public const decimal Threshold = 25m;

    public ObservationFamily Family => ObservationFamily.Timing;

    public int MinimumLoggedDays => ObservationThresholds.MinimumLoggedDays;

    public string ThresholdDescription => $"at least {Threshold}% of energy logged at or after {LateHour}:00 local";

    public Observation? Evaluate(AnalyticsFigures figures)
    {
        var share = figures.ByHour.ShareAtOrAfter(LateHour);

        if (share < Threshold)
            return null;

        return Observation.Create(
            Family,
            $"{share:0.#}% of what you logged was recorded at or after {LateHour}:00.",
            $"{share:0.#}%",
            ObservationThresholds.Strength(share, Threshold, ceiling: 60m),
            figures.Period.LoggedDays);
    }
}

/// <summary>
/// Weekend days running noticeably heavier than weekdays.
/// </summary>
/// <remarks>
/// Reviewed against FR-019: reports the gap in calories. It does not call the weekend a lapse.
/// <para>
/// Needs both kinds of day present in useful numbers, not just the overall minimum — one logged
/// Saturday against one logged Tuesday is a comparison of two days wearing the language of a
/// pattern.
/// </para>
/// </remarks>
public class WeekendHeavierRule : IObservationRule
{
    public const decimal Threshold = 20m;
    public const int MinimumWeekendDays = 2;
    public const int MinimumWeekdayDays = 4;

    public ObservationFamily Family => ObservationFamily.Timing;

    public int MinimumLoggedDays => ObservationThresholds.MinimumLoggedDays;

    public string ThresholdDescription =>
        $"weekend daily average at least {Threshold}% above the weekday average, "
        + $"with at least {MinimumWeekendDays} weekend and {MinimumWeekdayDays} weekday days logged";

    public Observation? Evaluate(AnalyticsFigures figures)
    {
        var weekend = figures.ByWeekday.WeekendAverage;
        var weekday = figures.ByWeekday.WeekdayAverage;

        if (weekend is null || weekday is null or <= 0)
            return null;

        if (figures.ByWeekday.LoggedDaysOn(DayOfWeek.Saturday, DayOfWeek.Sunday) < MinimumWeekendDays)
            return null;

        var weekdayCount = figures.ByWeekday.LoggedDaysOn(
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);

        if (weekdayCount < MinimumWeekdayDays)
            return null;

        var difference = weekend.Value - weekday.Value;
        var percentAbove = difference * 100m / weekday.Value;

        if (percentAbove < Threshold)
            return null;

        return Observation.Create(
            Family,
            $"Your Saturdays and Sundays averaged {difference} kcal above your weekdays.",
            $"{difference} kcal",
            ObservationThresholds.Strength(percentAbove, Threshold, ceiling: 60m),
            figures.Period.LoggedDays);
    }
}
