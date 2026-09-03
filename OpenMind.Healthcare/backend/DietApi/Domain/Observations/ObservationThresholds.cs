namespace DietApi.Domain.Observations;

/// <summary>
/// The floor every rule shares, and how far past its threshold a rule fired.
/// </summary>
public static class ObservationThresholds
{
    /// <summary>
    /// The fewest logged days any rule will speak on.
    /// </summary>
    /// <remarks>
    /// A fortnight. Below this a "pattern" is a handful of days, and a confident sentence about it
    /// is worse than silence — a member told their weekends run heavy on the strength of one
    /// Saturday has been misled by a feature that was trying to help. Individual rules may demand
    /// more; none may demand less (FR-018).
    /// </remarks>
    public const int MinimumLoggedDays = 14;

    /// <summary>
    /// How strongly a rule applies, from 0 at its threshold to 1 at the point past which more is
    /// not more interesting.
    /// </summary>
    /// <remarks>
    /// This is what orders the list, and it is deliberately a pure function of the figures: the
    /// same data produces the same order every time, with nothing sampled and nothing arbitrary
    /// (FR-020).
    /// </remarks>
    public static decimal Strength(decimal value, decimal threshold, decimal ceiling)
    {
        if (ceiling <= threshold)
            return 1m;

        var scaled = (value - threshold) / (ceiling - threshold);
        return Math.Clamp(Math.Round(scaled, 2), 0m, 1m);
    }
}
