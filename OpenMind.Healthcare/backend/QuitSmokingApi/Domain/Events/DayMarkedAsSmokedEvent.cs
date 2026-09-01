using DDD.BuildingBlocks;
using QuitSmokingApi.Domain.Entities;

namespace QuitSmokingApi.Domain.Events;

/// <summary>
/// Domain event raised when the user marks a day as smoked (a failed day)
/// </summary>
public class DayMarkedAsSmokedEvent(Guid journeyId, DateOnly date, int cigarettesSmoked, RelapseTrigger trigger) : IDomainEvent
{
    public Guid JourneyId { get; } = journeyId;
    public DateOnly Date { get; } = date;
    public int CigarettesSmoked { get; } = cigarettesSmoked;
    public RelapseTrigger Trigger { get; } = trigger;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
