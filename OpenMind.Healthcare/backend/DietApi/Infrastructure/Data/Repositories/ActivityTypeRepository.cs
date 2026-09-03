using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for the curated activity catalogue.
/// </summary>
/// <remarks>
/// A LIKE scan is enough at under a hundred rows, so no full-text extension is needed. Prefix
/// matches are ordered first because someone typing "run" wants running before "cross country
/// running with a rucksack".
/// </remarks>
public class ActivityTypeRepository(DietDbContext context) : IActivityTypeRepository
{
    public async Task<IReadOnlyList<ActivityType>> SearchAsync(
        string query, int limit = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var normalised = ActivityType.Normalise(query);
        var capped = Math.Clamp(limit, 1, 20);

        var matches = await context.ActivityTypes
            .Where(a => EF.Functions.Like(a.SearchName, $"%{normalised}%"))
            .ToListAsync(cancellationToken);

        return [.. matches
            .OrderByDescending(a => a.SearchName.StartsWith(normalised, StringComparison.Ordinal))
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Take(capped)];
    }

    public async Task<ActivityType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ActivityTypes
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}
