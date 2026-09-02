using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;

namespace DietApi.Tests.TestSupport;

/// <summary>
/// In-memory stand-in for the day store, keyed by member and date.
/// </summary>
public sealed class FakeLoggedDayRepository : ILoggedDayRepository
{
    private readonly List<LoggedDay> _days = [];

    public int SaveCount { get; private set; }
    public int DeleteCount { get; private set; }

    public static FakeLoggedDayRepository Empty() => new();

    public static FakeLoggedDayRepository Containing(params LoggedDay[] days)
    {
        var repository = new FakeLoggedDayRepository();
        repository._days.AddRange(days);
        return repository;
    }

    public IReadOnlyList<LoggedDay> Stored => _days;

    public Task<LoggedDay?> GetByDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) =>
        Task.FromResult(_days.SingleOrDefault(d => d.UserId == userId && d.Date == date));

    public Task<LoggedDay?> GetByEntryIdAsync(Guid userId, Guid entryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_days.SingleOrDefault(d => d.UserId == userId && d.Entries.Any(e => e.Id == entryId)));

    public Task<IReadOnlyList<DaySummary>> GetRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DaySummary> summaries =
        [
            .. _days
                .Where(d => d.UserId == userId && d.Date >= from && d.Date <= to)
                .OrderBy(d => d.Date)
                .Select(d => new DaySummary(d.Date, d.Totals.Calories, d.TargetSnapshot.Calories, !d.IsEmpty))
        ];

        return Task.FromResult(summaries);
    }

    public Task AddAsync(LoggedDay day, CancellationToken cancellationToken = default)
    {
        _days.Add(day);
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LoggedDay day, CancellationToken cancellationToken = default)
    {
        if (!_days.Contains(day))
            _days.Add(day);

        SaveCount++;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(LoggedDay day, CancellationToken cancellationToken = default)
    {
        _days.Remove(day);
        DeleteCount++;
        return Task.CompletedTask;
    }
}
