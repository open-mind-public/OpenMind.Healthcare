using DietApi.Domain.Aggregates;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Repositories;

/// <summary>
/// Repository interface for the curated eating tips.
/// </summary>
public interface IEatingTipRepository
{
    Task<IReadOnlyList<EatingTip>> GetAsync(TipCategory? category = null, CancellationToken cancellationToken = default);
}
