using DietApi.Domain.Aggregates;

namespace DietApi.Domain.Repositories;

/// <summary>
/// Repository interface for the <see cref="DietPlan"/> aggregate root.
/// </summary>
public interface IDietPlanRepository
{
    Task<DietPlan?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(DietPlan plan, CancellationToken cancellationToken = default);
    Task UpdateAsync(DietPlan plan, CancellationToken cancellationToken = default);
}
