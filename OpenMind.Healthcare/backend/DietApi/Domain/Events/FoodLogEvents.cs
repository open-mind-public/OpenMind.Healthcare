using DDD.BuildingBlocks;

namespace DietApi.Domain.Events;

public record FoodEntryLoggedEvent(Guid LoggedDayId, DateOnly Date, int Calories) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
