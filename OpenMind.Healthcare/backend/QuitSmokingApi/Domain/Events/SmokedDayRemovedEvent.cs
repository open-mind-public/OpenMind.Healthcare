using DDD.BuildingBlocks;

namespace QuitSmokingApi.Domain.Events;

/// <summary>
/// Domain event raised when a previously marked smoked day is removed (mistake correction)
/// </summary>
public class SmokedDayRemovedEvent(Guid journeyId, DateOnly date) : IDomainEvent
{
    public Guid JourneyId { get; } = journeyId;
    public DateOnly Date { get; } = date;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
