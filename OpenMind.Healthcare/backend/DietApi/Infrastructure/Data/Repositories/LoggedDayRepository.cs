using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for the <see cref="LoggedDay"/> aggregate root.
/// </summary>
public class LoggedDayRepository(DietDbContext context) : ILoggedDayRepository
{
    public async Task<LoggedDay?> GetByDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await context.LoggedDays
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == date, cancellationToken);
    }

    public async Task<LoggedDay?> GetByEntryIdAsync(Guid userId, Guid entryId, CancellationToken cancellationToken = default)
    {
        // Filtering by UserId as well as the entry id is what makes another member's entry
        // unreachable rather than merely forbidden.
        return await context.LoggedDays
            .Where(d => d.UserId == userId && d.Entries.Any(e => e.Id == entryId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DaySummary>> GetRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        // Projects to the summary in SQL: the entries never leave the database. Calories are int
        // columns precisely so this stays a numeric read.
        return await context.LoggedDays
            .Where(d => d.UserId == userId && d.Date >= from && d.Date <= to)
            .OrderBy(d => d.Date)
            .Select(d => new DaySummary(
                d.Date,
                d.Totals.Calories,
                d.TargetSnapshot.Calories,
                d.Entries.Any()))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(LoggedDay day, CancellationToken cancellationToken = default)
    {
        await context.LoggedDays.AddAsync(day, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LoggedDay day, CancellationToken cancellationToken = default)
    {
        // A tracked day already has its added/removed entries detected by the change tracker;
        // calling Update() on it would mark new child rows as Modified instead of Added.
        if (context.Entry(day).State == EntityState.Detached)
            context.LoggedDays.Update(day);

        // DbUpdateConcurrencyException is deliberately left to escape: the endpoint turns it into
        // a 409 so the member reloads, rather than one device silently overwriting the other.
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(LoggedDay day, CancellationToken cancellationToken = default)
    {
        context.LoggedDays.Remove(day);
        await context.SaveChangesAsync(cancellationToken);
    }
}
