using DietApi.Domain.Observations;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Services;

/// <summary>
/// Runs every observation rule and returns what survives.
/// </summary>
/// <remarks>
/// <para>
/// Four filters, in order, each answering a requirement:
/// </para>
/// <list type="number">
/// <item>A rule whose minimum exceeds the period's logged days is not consulted at all (FR-018).</item>
/// <item>A rule that returns null did not meet its threshold (FR-018).</item>
/// <item>Only the strongest of each family survives, so the list does not say one thing twice in
/// different words (FR-022).</item>
/// <item>What remains is ordered by strength, with a stable tie-break, so the same figures always
/// produce the same list in the same order (FR-020).</item>
/// </list>
/// <para>
/// Pure: no clock, no repository, no randomness. An empty result is a valid and meaningful answer —
/// it means nothing met its threshold, which the response states rather than leaving the client to
/// infer from a missing list (FR-021).
/// </para>
/// </remarks>
public class ObservationEngine(IEnumerable<IObservationRule> rules)
{
    private readonly IReadOnlyList<IObservationRule> _rules = [.. rules];

    /// <summary>Every rule registered, for the tests that assert properties across all of them.</summary>
    public IReadOnlyList<IObservationRule> Rules => _rules;

    /// <summary>The fewest logged days at which any rule could speak.</summary>
    public int MinimumDaysForAnyObservation =>
        _rules.Count == 0 ? 0 : _rules.Min(r => r.MinimumLoggedDays);

    public IReadOnlyList<Observation> Observe(AnalyticsFigures figures)
    {
        var found = new List<Observation>();

        foreach (var rule in _rules)
        {
            // Asked here rather than inside each rule, so a rule cannot fire on thin data even if
            // its own arithmetic would allow it.
            if (figures.Period.LoggedDays < rule.MinimumLoggedDays)
                continue;

            var observation = rule.Evaluate(figures);

            if (observation is not null)
                found.Add(observation);
        }

        return
        [
            .. found
                .GroupBy(o => o.Family)
                .Select(g => g.OrderByDescending(o => o.Strength).ThenBy(o => o.Text, StringComparer.Ordinal).First())
                .OrderByDescending(o => o.Strength)
                .ThenBy(o => o.Family)
        ];
    }
}
