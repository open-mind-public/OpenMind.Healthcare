using DietApi.Domain.Aggregates;

namespace DietApi.Domain.Repositories;

/// <summary>A day summary for calendar and statistics reads - never the entries.</summary>
public record DaySummary(DateOnly Date, int ConsumedCalories, int TargetCalories, bool HasEntries);

/// <summary>
/// Repository interface for the <see cref="LoggedDay"/> aggregate root.
/// </summary>
public interface ILoggedDayRepository
{
    Task<LoggedDay?> GetByDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

    Task<LoggedDay?> GetByEntryIdAsync(Guid userId, Guid entryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// One small row per logged day in the range. Deliberately does not load entries - a member
    /// with three years of history would otherwise pull thousands of rows to draw a calendar.
    /// </summary>
    Task<IReadOnlyList<DaySummary>> GetRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task AddAsync(LoggedDay day, CancellationToken cancellationToken = default);
    Task UpdateAsync(LoggedDay day, CancellationToken cancellationToken = default);
    Task DeleteAsync(LoggedDay day, CancellationToken cancellationToken = default);
}
