using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Services;

/// <summary>
/// Turns a preset into the window that will actually be analysed.
/// </summary>
/// <remarks>
/// <para>
/// The only place in this feature that reads "today", and it takes it as a parameter. Everything
/// downstream works from the resolved period, which is what makes the whole feature testable
/// across arbitrary dates (Principle IV).
/// </para>
/// <para>
/// Clamping is the point. A member who started their plan four days ago and asks for the last
/// quarter gets four days, flagged as narrowed, rather than eighty-six days of nothing dressed up
/// as a quiet period (FR-002).
/// </para>
/// </remarks>
public class AnalysisPeriodResolver
{
    /// <summary>How many days each preset reaches back, inclusive of today.</summary>
    public static int WindowDays(PeriodPreset preset) => preset switch
    {
        PeriodPreset.Week => 7,
        PeriodPreset.Month => 30,
        PeriodPreset.Quarter => 90,
        _ => 0
    };

    public AnalysisPeriod Resolve(PeriodPreset preset, DateOnly planStartDate, DateTime? asOf = null)
    {
        var today = DateOnly.FromDateTime(asOf ?? DateTime.UtcNow);

        // A plan that starts in the future would leave nothing to analyse; treat its start as
        // today so the period is one day rather than an error the member cannot act on.
        var effectiveStart = planStartDate > today ? today : planStartDate;

        var requestedFrom = preset == PeriodPreset.Plan
            ? effectiveStart
            : today.AddDays(-(WindowDays(preset) - 1));

        var from = requestedFrom < effectiveStart ? effectiveStart : requestedFrom;
        var wasNarrowed = from != requestedFrom;

        var (previousFrom, previousTo) = PreviousWindow(preset, from, effectiveStart);

        return AnalysisPeriod.Create(
            preset, from, today, effectiveStart, today, wasNarrowed, previousFrom, previousTo);
    }

    /// <summary>
    /// The span of the same length immediately before the window, or nothing.
    /// </summary>
    /// <remarks>
    /// There is deliberately no comparison for the whole-plan preset - there is nothing before a
    /// plan started - and none when the preceding span would fall partly outside the plan. A
    /// partial window compared against a full one would report a fall that is an artefact of the
    /// member having joined recently, which is worse than reporting nothing.
    /// </remarks>
    private static (DateOnly? From, DateOnly? To) PreviousWindow(
        PeriodPreset preset, DateOnly from, DateOnly planStartDate)
    {
        if (preset == PeriodPreset.Plan)
            return (null, null);

        var previousTo = from.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(WindowDays(preset) - 1));

        return previousFrom < planStartDate ? (null, null) : (previousFrom, previousTo);
    }
}
