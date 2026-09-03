using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Observations;

/// <summary>
/// One thing the programme knows how to notice.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MinimumLoggedDays"/> is declared data rather than being buried inside
/// <see cref="Evaluate"/>. That is what lets a single test assert, over every rule that exists or
/// will exist, that none of them speaks below its own minimum — without the test needing to know
/// what any rule says (FR-018, SC-008).
/// </para>
/// <para>
/// <see cref="Evaluate"/> must be a pure function of its input: the same figures give the same
/// observation, every time, with no clock and no randomness (FR-020).
/// </para>
/// <para>
/// Wording is fixed, with the figure interpolated. Every rule's sentence is reviewed against
/// FR-019 before release: it may describe what the data shows, and it may not diagnose a
/// condition, judge the member, or tell them what to eat.
/// </para>
/// </remarks>
public interface IObservationRule
{
    /// <summary>What this rule is about. Only the strongest of a family is shown (FR-022).</summary>
    ObservationFamily Family { get; }

    /// <summary>Below this many logged days the rule is not consulted at all.</summary>
    int MinimumLoggedDays { get; }

    /// <summary>The threshold in words, for the pre-release review of every rule.</summary>
    string ThresholdDescription { get; }

    /// <summary>The observation, or null when the threshold is not met.</summary>
    Observation? Evaluate(AnalyticsFigures figures);
}
