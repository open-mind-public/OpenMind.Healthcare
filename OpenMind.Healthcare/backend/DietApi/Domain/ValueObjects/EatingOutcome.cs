namespace DietApi.Domain.ValueObjects;

/// <summary>
/// How one group of days stood against target: the counts, and the same counts as fractions of the
/// group.
/// </summary>
/// <remarks>
/// The shares are carried alongside the counts so a client compares two groups of different sizes
/// without dividing anything itself. A group with no days is all zeros - never a divide-by-zero and
/// never a blank (FR-015).
/// </remarks>
public record EatingOutcome(
    int Days,
    int OnTargetDays,
    int OverTargetDays,
    int NotLoggedDays,
    decimal OnTargetShare,
    decimal OverTargetShare,
    decimal NotLoggedShare)
{
    public static EatingOutcome From(int onTargetDays, int overTargetDays, int notLoggedDays)
    {
        var days = onTargetDays + overTargetDays + notLoggedDays;

        return days == 0
            ? new EatingOutcome(0, 0, 0, 0, 0m, 0m, 0m)
            : new EatingOutcome(
                days,
                onTargetDays,
                overTargetDays,
                notLoggedDays,
                Share(onTargetDays, days),
                Share(overTargetDays, days),
                Share(notLoggedDays, days));
    }

    /// <summary>A group a member has no days in yet.</summary>
    public static EatingOutcome Empty { get; } = new(0, 0, 0, 0, 0m, 0m, 0m);

    private static decimal Share(int part, int whole) => Math.Round((decimal)part / whole, 2);
}
