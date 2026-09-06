using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;

namespace DietApi.Tests.TestSupport;

/// <summary>
/// In-memory stand-in for the beer day store, keyed by member and date.
/// </summary>
public sealed class FakeBeerDayRepository : IBeerDayRepository
{
    private readonly List<BeerDay> _days = [];

    public int SaveCount { get; private set; }
    public int DeleteCount { get; private set; }

    public static FakeBeerDayRepository Empty() => new();

    public static FakeBeerDayRepository Containing(params BeerDay[] days)
    {
        var repository = new FakeBeerDayRepository();
        repository._days.AddRange(days);
        return repository;
    }

    public IReadOnlyList<BeerDay> Stored => _days;

    public Task<BeerDay?> GetByDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) =>
        Task.FromResult(_days.SingleOrDefault(d => d.UserId == userId && d.Date == date));

    public Task<IReadOnlyList<DateOnly>> GetDatesInRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DateOnly> dates =
        [
            .. _days
                .Where(d => d.UserId == userId && d.Date >= from && d.Date <= to)
                .OrderBy(d => d.Date)
                .Select(d => d.Date)
        ];

        return Task.FromResult(dates);
    }

    public Task AddAsync(BeerDay day, CancellationToken cancellationToken = default)
    {
        _days.Add(day);
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(BeerDay day, CancellationToken cancellationToken = default)
    {
        _days.Remove(day);
        DeleteCount++;
        return Task.CompletedTask;
    }
}
