using DietApi.Domain.Aggregates;

namespace DietApi.Domain.Repositories;

/// <summary>
/// Repository interface for the curated activity catalogue.
/// </summary>
public interface IActivityTypeRepository
{
    /// <summary>
    /// Case-insensitive match on the normalised name, prefix matches first then alphabetically.
    /// An empty result is how a member learns an activity is not in the catalogue.
    /// </summary>
    Task<IReadOnlyList<ActivityType>> SearchAsync(
        string query, int limit = 20, CancellationToken cancellationToken = default);

    Task<ActivityType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
