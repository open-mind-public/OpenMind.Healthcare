using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Services;

/// <summary>
/// Averages a period's macronutrients against the targets that were in force.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Sums in memory, never in SQL.</strong> Macronutrient grams are <c>decimal</c>, which EF
/// Core maps to SQLite <c>TEXT</c>, and ADR 0002 forbids aggregating those in the database. A probe
/// run while planning this feature found <c>SUM</c> over such a column returning a <em>correct</em>
/// answer on two clean rows — which is exactly what makes it dangerous. It would pass every test
/// written over a handful of days and drift on a value that does not round-trip cleanly through
/// text. Three years is at most ~1,100 day rows, so complying costs nothing measurable.
/// </para>
/// <para>
/// The target is averaged from each day's own stored snapshot rather than read from the plan. A
/// member who lowered their target mid-month is compared against what they were actually aiming
/// for on each day, not against today's figure applied retrospectively (FR-011).
/// </para>
/// </remarks>
public class MacronutrientAnalyser
{
    public MacronutrientComparison Analyse(IReadOnlyList<DayIntakeRow> days)
    {
        if (days.Count == 0)
            return MacronutrientComparison.Empty();

        var count = days.Count;

        return MacronutrientComparison.Create(
            proteinG: days.Sum(d => d.ProteinG) / count,
            carbsG: days.Sum(d => d.CarbsG) / count,
            fatG: days.Sum(d => d.FatG) / count,
            averagedOverDays: count,
            targetProteinG: AverageTarget(days, d => d.TargetProteinG),
            targetCarbsG: AverageTarget(days, d => d.TargetCarbsG),
            targetFatG: AverageTarget(days, d => d.TargetFatG));
    }

    /// <summary>
    /// The average target across the days that actually carried one.
    /// </summary>
    /// <remarks>
    /// Days with no target for a macronutrient are excluded from its average rather than counted
    /// as zero. Counting them as zero would drag the target down and make a member look closer to
    /// it than they were — inventing a comparison out of an absence, which FR-012 forbids.
    /// </remarks>
    private static decimal? AverageTarget(
        IReadOnlyList<DayIntakeRow> days, Func<DayIntakeRow, decimal?> target)
    {
        var stated = days.Select(target).Where(t => t.HasValue).Select(t => t!.Value).ToList();

        return stated.Count == 0 ? null : stated.Sum() / stated.Count;
    }
}
