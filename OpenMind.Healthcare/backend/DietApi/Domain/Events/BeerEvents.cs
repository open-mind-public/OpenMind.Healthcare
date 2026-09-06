using DDD.BuildingBlocks;

namespace DietApi.Domain.Events;

/// <summary>
/// Raised when a member marks a date as a beer day.
/// </summary>
/// <remarks>
/// Carries the date and nothing else - a beer day records only that beer was consumed, no amount
/// and no calorie figure. No handler in this context reacts to it by touching a target, a logged
/// day, a streak, or any average; that absence is the guarantee the feature is shaped around
/// (FR-004, FR-010).
/// </remarks>
public record BeerDayMarkedEvent(Guid BeerDayId, DateOnly Date) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
