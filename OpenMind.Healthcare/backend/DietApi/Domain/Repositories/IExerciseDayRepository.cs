using DietApi.Domain.Aggregates;

namespace DietApi.Domain.Repositories;

/// <summary>One day's activity for calendar and summary reads - never the sessions themselves.</summary>
public record ExerciseDaySummary(DateOnly Date, int TotalMinutes, int TotalKilocalories, int EntryCount);

/// <summary>
/// Repository interface for the <see cref="ExerciseDay"/> aggregate root.
/// </summary>
public interface IExerciseDayRepository
{
    Task<ExerciseDay?> GetByDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

    Task<ExerciseDay?> GetByEntryIdAsync(Guid userId, Guid entryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// One small row per day <em>that has sessions</em>. Absence means no exercise, which is what
    /// lets the calendar mark days without inventing a state for the rest.
    /// </summary>
    Task<IReadOnlyList<ExerciseDaySummary>> GetRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task AddAsync(ExerciseDay day, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExerciseDay day, CancellationToken cancellationToken = default);
    Task DeleteAsync(ExerciseDay day, CancellationToken cancellationToken = default);
}
