using DDD.BuildingBlocks;

namespace DietApi.Domain.ValueObjects;

/// <summary>
/// One calendar day on the trend.
/// </summary>
/// <remarks>
/// <see cref="Logged"/> is the field that matters. A day the member did not log is a gap, not a
/// zero: drawing a line through it would show intake that never happened, which is the exact lie
/// this feature is built to avoid. The figures on an unlogged point are meaningless and the chart
/// must not join across it.
/// <para>
/// The <em>target</em> is different, and is carried forward across gaps deliberately. A member's
/// target existed on days they did not log — not logging does not suspend it — so the reference
/// line is continuous where the intake line is not.
/// </para>
/// </remarks>
public record DailyIntakePoint(
    DateOnly Date,
    bool Logged,
    int Calories,
    int TargetCalories,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG,
    decimal? TargetProteinG,
    decimal? TargetCarbsG,
    decimal? TargetFatG);

/// <summary>
/// A member's intake day by day across a period, with unlogged days left as gaps.
/// </summary>
public class IntakeTrend : ValueObject
{
    public IReadOnlyList<DailyIntakePoint> Points { get; private set; } = [];

    private IntakeTrend() { }

    public static IntakeTrend Create(IReadOnlyList<DailyIntakePoint> points) =>
        new() { Points = [.. points.OrderBy(p => p.Date)] };

    public static IntakeTrend Empty() => new() { Points = [] };

    public int LoggedDays => Points.Count(p => p.Logged);

    /// <summary>The largest figure the chart has to fit, so an axis can be scaled once.</summary>
    public int PeakCalories =>
        Points.Count == 0 ? 0 : Math.Max(Points.Max(p => p.Calories), Points.Max(p => p.TargetCalories));

    protected override IEnumerable<object?> GetEqualityComponents() => Points.Cast<object?>();
}
