using DDD.BuildingBlocks;

namespace DietApi.Domain.Entities;

/// <summary>
/// A member's weight on a given date. Part of the <c>DietPlan</c> aggregate - at most one per
/// date, and only mutable through the aggregate's <c>RecordWeight</c> and
/// <c>RemoveWeightReading</c> methods.
/// </summary>
public class WeightReading : Entity
{
    public Guid DietPlanId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal WeightKg { get; private set; }
    public DateTime RecordedAt { get; private set; }

    // Private parameterless constructor for EF Core
    private WeightReading() { }

    internal static WeightReading Record(Guid dietPlanId, DateOnly date, decimal weightKg, DateTime recordedAt) =>
        new()
        {
            DietPlanId = dietPlanId,
            Date = date,
            WeightKg = Math.Round(weightKg, 2),
            RecordedAt = recordedAt
        };
}
