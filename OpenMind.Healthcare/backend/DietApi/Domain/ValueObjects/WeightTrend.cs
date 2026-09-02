using DDD.BuildingBlocks;
using DietApi.Domain.Entities;

namespace DietApi.Domain.ValueObjects;

/// <summary>
/// A member's weight over a chosen period, with the two figures they actually care about: how far
/// they have come, and how far is left.
/// </summary>
public class WeightTrend : ValueObject
{
    public IReadOnlyList<WeightReading> Readings { get; private set; } = [];

    /// <summary>The reading nearest the plan's start date - the baseline change is measured from.</summary>
    public decimal? StartWeightKg { get; private set; }

    public decimal? CurrentWeightKg { get; private set; }

    /// <summary>Negative when the member has lost weight.</summary>
    public decimal? ChangeKg { get; private set; }

    public decimal? TargetWeightKg { get; private set; }

    /// <summary>Always reported as a distance, never a negative "overshoot".</summary>
    public decimal? RemainingToTargetKg { get; private set; }

    public bool GoalReached { get; private set; }

    private WeightTrend() { }

    public static WeightTrend Create(
        IReadOnlyList<WeightReading> readings,
        decimal? startWeightKg,
        decimal? currentWeightKg,
        decimal? targetWeightKg,
        GoalType goal)
    {
        var change = startWeightKg is not null && currentWeightKg is not null
            ? Math.Round(currentWeightKg.Value - startWeightKg.Value, 2)
            : (decimal?)null;

        decimal? remaining = null;
        var reached = false;

        if (targetWeightKg is not null && currentWeightKg is not null)
        {
            remaining = Math.Round(Math.Abs(targetWeightKg.Value - currentWeightKg.Value), 2);

            // Reaching the goal means passing it in the direction being travelled. A member
            // aiming to lose who has gone below their target has arrived, not overshot.
            reached = goal switch
            {
                GoalType.LoseWeight => currentWeightKg <= targetWeightKg,
                GoalType.GainWeight => currentWeightKg >= targetWeightKg,
                _ => remaining <= 0.5m
            };
        }

        return new WeightTrend
        {
            Readings = readings,
            StartWeightKg = startWeightKg,
            CurrentWeightKg = currentWeightKg,
            ChangeKg = change,
            TargetWeightKg = targetWeightKg,
            RemainingToTargetKg = remaining,
            GoalReached = reached
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartWeightKg;
        yield return CurrentWeightKg;
        yield return TargetWeightKg;
        yield return GoalReached;
    }
}
