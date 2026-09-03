using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for the <see cref="ExerciseDay"/> aggregate root.
/// </summary>
public class ExerciseDayRepository(DietDbContext context) : IExerciseDayRepository
{
    public async Task<ExerciseDay?> GetByDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await context.ExerciseDays
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == date, cancellationToken);
    }

    public async Task<ExerciseDay?> GetByEntryIdAsync(Guid userId, Guid entryId, CancellationToken cancellationToken = default)
    {
        // Filtering by UserId as well as the entry id is what makes another member's session
        // unreachable rather than merely forbidden.
        return await context.ExerciseDays
            .Where(d => d.UserId == userId && d.Entries.Any(e => e.Id == entryId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExerciseDaySummary>> GetRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        // Projects to the summary in SQL: the sessions never leave the database. Minutes and
        // kilocalories are int columns precisely so this stays a numeric read (ADR 0002).
        return await context.ExerciseDays
            .Where(d => d.UserId == userId && d.Date >= from && d.Date <= to)
            .OrderBy(d => d.Date)
            .Select(d => new ExerciseDaySummary(
                d.Date,
                d.Totals.Minutes,
                d.Totals.Kilocalories,
                d.Entries.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ExerciseDay day, CancellationToken cancellationToken = default)
    {
        await context.ExerciseDays.AddAsync(day, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ExerciseDay day, CancellationToken cancellationToken = default)
    {
        // A tracked day already has its added/removed sessions detected by the change tracker;
        // calling Update() on it would mark new child rows as Modified instead of Added.
        if (context.Entry(day).State == EntityState.Detached)
            context.ExerciseDays.Update(day);

        // DbUpdateConcurrencyException is deliberately left to escape: the endpoint turns it into
        // a 409 so the member reloads, rather than one device silently overwriting the other.
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ExerciseDay day, CancellationToken cancellationToken = default)
    {
        context.ExerciseDays.Remove(day);
        await context.SaveChangesAsync(cancellationToken);
    }
}
