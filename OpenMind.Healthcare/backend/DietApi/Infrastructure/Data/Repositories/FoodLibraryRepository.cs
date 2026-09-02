using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for the curated food library.
/// </summary>
/// <remarks>
/// A LIKE scan is enough at a couple of hundred rows, so no full-text extension is needed.
/// Prefix matches are ordered first because someone typing "oat" wants porridge oats before
/// "goat cheese".
/// </remarks>
public class FoodLibraryRepository(DietDbContext context) : IFoodLibraryRepository
{
    public async Task<IReadOnlyList<FoodLibraryItem>> SearchAsync(
        string query, int limit = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var normalised = FoodLibraryItem.Normalise(query);
        var capped = Math.Clamp(limit, 1, 20);

        var matches = await context.FoodLibraryItems
            .Where(f => EF.Functions.Like(f.SearchName, $"%{normalised}%"))
            .ToListAsync(cancellationToken);

        return [.. matches
            .OrderByDescending(f => f.SearchName.StartsWith(normalised, StringComparison.Ordinal))
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .Take(capped)];
    }

    public async Task<FoodLibraryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.FoodLibraryItems
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }
}
