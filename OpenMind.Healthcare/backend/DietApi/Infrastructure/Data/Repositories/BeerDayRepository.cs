using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for the <see cref="BeerDay"/> aggregate root.
/// </summary>
public class BeerDayRepository(DietDbContext context) : IBeerDayRepository
{
    public async Task<BeerDay?> GetByDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await context.BeerDays
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == date, cancellationToken);
    }

    public async Task<IReadOnlyList<DateOnly>> GetDatesInRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        // Projects to the date in SQL: the aggregates never leave the database, and filtering by
        // UserId is what makes another member's beer days unreachable rather than merely forbidden.
        return await context.BeerDays
            .Where(d => d.UserId == userId && d.Date >= from && d.Date <= to)
            .OrderBy(d => d.Date)
            .Select(d => d.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(BeerDay day, CancellationToken cancellationToken = default)
    {
        await context.BeerDays.AddAsync(day, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(BeerDay day, CancellationToken cancellationToken = default)
    {
        context.BeerDays.Remove(day);
        await context.SaveChangesAsync(cancellationToken);
    }
}
