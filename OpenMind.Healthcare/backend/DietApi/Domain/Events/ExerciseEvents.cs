using DDD.BuildingBlocks;

namespace DietApi.Domain.Events;

/// <summary>
/// Raised when a member records a session.
/// </summary>
/// <remarks>
/// Carries the estimate for the benefit of anything watching activity, and nothing that could be
/// mistaken for a calorie allowance. No handler in this context adjusts a target or a day's
/// assessment in response - that is the guarantee the whole feature is shaped around (FR-015).
/// </remarks>
public record ExerciseLoggedEvent(Guid ExerciseDayId, DateOnly Date, int DurationMinutes, int EstimatedKcal)
    : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
