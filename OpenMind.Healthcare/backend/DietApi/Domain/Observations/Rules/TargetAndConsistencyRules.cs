using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Observations.Rules;

/// <summary>
/// Protein averaging well below the target the member set.
/// </summary>
/// <remarks>
/// Reviewed against FR-019: compares two numbers the member chose and the member ate. It does not
/// tell them to eat more protein, and it says nothing at all when they set no protein target -
/// there is no default target to fall back on, because a target nobody chose is not a target
/// (FR-012).
/// </remarks>
public class ProteinBelowTargetRule : IObservationRule
{
    /// <summary>Below four fifths of target is a gap worth naming; nearer than that is noise.</summary>
    public const decimal Threshold = 0.8m;

    public ObservationFamily Family => ObservationFamily.Targets;

    public int MinimumLoggedDays => ObservationThresholds.MinimumLoggedDays;

    public string ThresholdDescription =>
        $"average protein at or below {Threshold:P0} of the average target, when a protein target is set";

    public Observation? Evaluate(AnalyticsFigures figures)
    {
        var attainment = figures.Macronutrients.ProteinAttainment;

        if (attainment is null || attainment > Threshold)
            return null;

        var actual = figures.Macronutrients.ProteinG;
        var target = figures.Macronutrients.TargetProteinG!.Value;

        return Observation.Create(
            Family,
            $"You averaged {actual:0.#} g of protein a day against a target of {target:0.#} g.",
            $"{actual:0.#} g",

            // Inverted: the further below target, the stronger. Half of target is the ceiling.
            ObservationThresholds.Strength(Threshold - attainment.Value, 0m, ceiling: Threshold - 0.5m),
            figures.Macronutrients.AveragedOverDays);
    }
}

/// <summary>
/// Logging more days than in the period before.
/// </summary>
/// <remarks>
/// Reviewed against FR-019: reports two counts. The only rule of the seven that has anything good
/// to say, which is deliberate - a list of observations that only ever names shortfalls reads as
/// criticism however carefully each sentence is worded.
/// </remarks>
public class LoggingImprovedRule : IObservationRule
{
    public const decimal Threshold = 25m;

    public ObservationFamily Family => ObservationFamily.Consistency;

    public int MinimumLoggedDays => ObservationThresholds.MinimumLoggedDays;

    public string ThresholdDescription =>
        $"logged days at least {Threshold}% above the previous window, with both windows meeting the minimum";

    public Observation? Evaluate(AnalyticsFigures figures)
    {
        if (!figures.Period.HasComparison || figures.PreviousLoggedDays <= 0)
            return null;

        var now = figures.Period.LoggedDays;
        var before = figures.PreviousLoggedDays;

        if (now <= before)
            return null;

        var percentUp = (now - before) * 100m / before;

        if (percentUp < Threshold)
            return null;

        return Observation.Create(
            Family,
            $"You logged {now} days this period, up from {before}.",
            $"{now} days",
            ObservationThresholds.Strength(percentUp, Threshold, ceiling: 100m),
            now);
    }
}
