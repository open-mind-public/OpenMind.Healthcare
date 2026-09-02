using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;

namespace DietApi.Tests.TestSupport;

/// <summary>
/// In-memory stand-in for the plan store. <see cref="SaveCount"/> lets a test see whether a
/// command actually persisted its work - the one thing an in-memory double cannot show by
/// re-reading.
/// </summary>
public sealed class FakeDietPlanRepository : IDietPlanRepository
{
    private readonly Dictionary<Guid, DietPlan> _plansByUser = [];

    public int SaveCount { get; private set; }

    public static FakeDietPlanRepository Empty() => new();

    public static FakeDietPlanRepository Containing(DietPlan plan)
    {
        var repository = new FakeDietPlanRepository();
        repository._plansByUser[plan.UserId] = plan;
        return repository;
    }

    public DietPlan? StoredFor(Guid userId) => _plansByUser.GetValueOrDefault(userId);

    public Task<DietPlan?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_plansByUser.GetValueOrDefault(userId));

    public Task AddAsync(DietPlan plan, CancellationToken cancellationToken = default)
    {
        _plansByUser[plan.UserId] = plan;
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DietPlan plan, CancellationToken cancellationToken = default)
    {
        _plansByUser[plan.UserId] = plan;
        SaveCount++;
        return Task.CompletedTask;
    }
}
