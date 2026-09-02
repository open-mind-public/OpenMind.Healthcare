using DietApi.Domain.Aggregates;

namespace DietApi.Domain.Repositories;

/// <summary>
/// Repository interface for the curated food library.
/// </summary>
public interface IFoodLibraryRepository
{
    /// <summary>
    /// Case-insensitive match on the normalised name, prefix matches first then alphabetically.
    /// An empty result is how a member learns a food is not in the library.
    /// </summary>
    Task<IReadOnlyList<FoodLibraryItem>> SearchAsync(
        string query, int limit = 20, CancellationToken cancellationToken = default);

    Task<FoodLibraryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
