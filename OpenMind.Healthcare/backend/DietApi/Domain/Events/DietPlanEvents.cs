using DDD.BuildingBlocks;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Events;

public record DietPlanCreatedEvent(Guid PlanId, Guid UserId, DateOnly StartDate) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TargetsChangedEvent(Guid PlanId, int Calories, TargetSource Source) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record WeightRecordedEvent(Guid PlanId, DateOnly Date, decimal WeightKg) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
