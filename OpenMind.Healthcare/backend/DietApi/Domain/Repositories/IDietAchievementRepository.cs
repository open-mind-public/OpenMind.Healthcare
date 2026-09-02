using DietApi.Domain.Aggregates;

namespace DietApi.Domain.Repositories;

/// <summary>
/// Repository interface for the seeded achievement definitions.
/// </summary>
public interface IDietAchievementRepository
{
    Task<IReadOnlyList<DietAchievement>> GetAllAsync(CancellationToken cancellationToken = default);
}
