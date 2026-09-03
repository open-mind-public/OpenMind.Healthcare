using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.TestSupport;

/// <summary>
/// In-memory stand-in for the curated activity catalogue, with a few known activities tests can
/// record.
/// </summary>
public sealed class FakeActivityTypeRepository : IActivityTypeRepository
{
    private readonly List<ActivityType> _activities = [];

    public static FakeActivityTypeRepository Empty() => new();

    public static FakeActivityTypeRepository Containing(params ActivityType[] activities)
    {
        var repository = new FakeActivityTypeRepository();
        repository._activities.AddRange(activities);
        return repository;
    }

    /// <summary>Running at 8 km/h - the MET value the worked example in research.md R-003 uses.</summary>
    public static ActivityType Running() =>
        ActivityType.Create("Running, 8 km/h", ActivityCategory.Running, 8.3m);

    /// <summary>A brisk walk, low enough that short sessions test the 1 kcal floor.</summary>
    public static ActivityType BriskWalk() =>
        ActivityType.Create("Walking, brisk (5.5 km/h)", ActivityCategory.Walking, 4.3m);

    /// <summary>Gentle enough that a one-minute session rounds to nothing without the floor.</summary>
    public static ActivityType Stretching() =>
        ActivityType.Create("Stretching, general", ActivityCategory.Gym, 2.3m);

    /// <summary>Deliberately punishing, for exercising the duration ceiling at scale.</summary>
    public static ActivityType Butterfly() =>
        ActivityType.Create("Swimming, butterfly", ActivityCategory.Swimming, 13.8m);

    public Task<IReadOnlyList<ActivityType>> SearchAsync(
        string query, int limit = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult<IReadOnlyList<ActivityType>>([]);

        var normalised = ActivityType.Normalise(query);

        IReadOnlyList<ActivityType> matches =
        [
            .. _activities
                .Where(a => a.SearchName.Contains(normalised, StringComparison.Ordinal))
                .OrderByDescending(a => a.SearchName.StartsWith(normalised, StringComparison.Ordinal))
                .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Clamp(limit, 1, 20))
        ];

        return Task.FromResult(matches);
    }

    public Task<ActivityType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_activities.SingleOrDefault(a => a.Id == id));
}
