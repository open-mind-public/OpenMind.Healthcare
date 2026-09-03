namespace DietApi.Domain.Services;

/// <summary>
/// Turns a session into an estimate of the energy it used.
/// </summary>
/// <remarks>
/// <para>
/// The standard metabolic-equivalent formula: kilocalories per hour equal the activity's MET
/// value multiplied by body weight in kilograms, so a session's cost is
/// <c>MET x weight x hours</c>. It is an estimate, not a measurement - two people of the same
/// weight doing the same run differ by more than this formula can express - which is why every
/// screen that shows the number labels it as one (FR-008).
/// </para>
/// <para>
/// Pure by design: no clock, no repository, no dependency. That is what makes it testable at its
/// boundary values, and what keeps the estimate identical whether it is computed on a write or
/// re-computed in a test.
/// </para>
/// </remarks>
public class EnergyEstimator
{
    /// <summary>
    /// A session a member bothered to record never reads as zero. Twelve minutes of gentle
    /// stretching at 45 kg genuinely rounds to nothing, and showing "0 kcal" would read as the
    /// app failing rather than the arithmetic being honest.
    /// </summary>
    public const int FloorKcal = 1;

    public int Estimate(decimal met, int durationMinutes, decimal weightKg)
    {
        if (durationMinutes <= 0)
            return 0;

        var kilocalories = met * weightKg * (durationMinutes / 60m);
        var rounded = (int)Math.Round(kilocalories, MidpointRounding.AwayFromZero);

        return Math.Max(rounded, FloorKcal);
    }
}
