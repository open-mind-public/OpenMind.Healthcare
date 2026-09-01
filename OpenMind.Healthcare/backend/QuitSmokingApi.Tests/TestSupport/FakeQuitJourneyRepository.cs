using QuitSmokingApi.Domain.Aggregates;
using QuitSmokingApi.Domain.Repositories;

namespace QuitSmokingApi.Tests.TestSupport;

/// <summary>
/// In-memory stand-in for the journey store. <see cref="SaveCount"/> lets a test see whether a
/// command actually persisted its work - the one thing an in-memory double cannot show by re-reading.
/// </summary>
public sealed class FakeQuitJourneyRepository : IQuitJourneyRepository
{
    private readonly Dictionary<Guid, QuitJourney> _journeysByUser = [];

    public int SaveCount { get; private set; }

    public static FakeQuitJourneyRepository Empty() => new();

    public static FakeQuitJourneyRepository Containing(QuitJourney journey)
    {
        var repository = new FakeQuitJourneyRepository();
        repository._journeysByUser[journey.UserId] = journey;
        return repository;
    }

    public QuitJourney? StoredFor(Guid userId) =>
        _journeysByUser.GetValueOrDefault(userId);

    public Task<QuitJourney?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_journeysByUser.GetValueOrDefault(userId));

    public Task AddAsync(QuitJourney journey, CancellationToken cancellationToken = default)
    {
        _journeysByUser[journey.UserId] = journey;
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(QuitJourney journey, CancellationToken cancellationToken = default)
    {
        _journeysByUser[journey.UserId] = journey;
        SaveCount++;
        return Task.CompletedTask;
    }
}
