using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;

namespace DietApi.Tests.TestSupport;

/// <summary>
/// In-memory stand-in for the exercise day store, keyed by member and date.
/// </summary>
public sealed class FakeExerciseDayRepository : IExerciseDayRepository
{
    private readonly List<ExerciseDay> _days = [];

    public int SaveCount { get; private set; }
    public int DeleteCount { get; private set; }

    public static FakeExerciseDayRepository Empty() => new();

    public static FakeExerciseDayRepository Containing(params ExerciseDay[] days)
    {
        var repository = new FakeExerciseDayRepository();
        repository._days.AddRange(days);
        return repository;
    }

    public IReadOnlyList<ExerciseDay> Stored => _days;

    public Task<ExerciseDay?> GetByDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) =>
        Task.FromResult(_days.SingleOrDefault(d => d.UserId == userId && d.Date == date));

    public Task<ExerciseDay?> GetByEntryIdAsync(Guid userId, Guid entryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_days.SingleOrDefault(d => d.UserId == userId && d.Entries.Any(e => e.Id == entryId)));

    public Task<IReadOnlyList<ExerciseDaySummary>> GetRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        // Only days that have sessions, mirroring the real repository - absence is how the
        // calendar learns a date had no exercise.
        IReadOnlyList<ExerciseDaySummary> summaries =
        [
            .. _days
                .Where(d => d.UserId == userId && d.Date >= from && d.Date <= to)
                .OrderBy(d => d.Date)
                .Select(d => new ExerciseDaySummary(d.Date, d.Totals.Minutes, d.Totals.Kilocalories, d.Entries.Count))
        ];

        return Task.FromResult(summaries);
    }

    public Task AddAsync(ExerciseDay day, CancellationToken cancellationToken = default)
    {
        _days.Add(day);
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ExerciseDay day, CancellationToken cancellationToken = default)
    {
        if (!_days.Contains(day))
            _days.Add(day);

        SaveCount++;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ExerciseDay day, CancellationToken cancellationToken = default)
    {
        _days.Remove(day);
        DeleteCount++;
        return Task.CompletedTask;
    }
}
