using DietApi.Domain.Aggregates;

namespace DietApi.Domain.Repositories;

/// <summary>
/// Repository interface for the <see cref="BeerDay"/> aggregate root.
/// </summary>
public interface IBeerDayRepository
{
    Task<BeerDay?> GetByDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// The dates in the range that are beer days - never the aggregates themselves. Absence of a
    /// date means it is not a beer day; there is no "not a beer day" row to be mistaken for a state.
    /// </summary>
    Task<IReadOnlyList<DateOnly>> GetDatesInRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task AddAsync(BeerDay day, CancellationToken cancellationToken = default);

    Task DeleteAsync(BeerDay day, CancellationToken cancellationToken = default);
}
