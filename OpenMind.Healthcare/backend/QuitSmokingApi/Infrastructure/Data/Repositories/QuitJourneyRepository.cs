using Microsoft.EntityFrameworkCore;
using QuitSmokingApi.Domain.Aggregates;
using QuitSmokingApi.Domain.Repositories;

namespace QuitSmokingApi.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for the QuitJourney aggregate root.
/// </summary>
public class QuitJourneyRepository(AppDbContext context) : IQuitJourneyRepository
{
    public async Task<QuitJourney?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.QuitJourneys
            .FirstOrDefaultAsync(j => j.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(QuitJourney journey, CancellationToken cancellationToken = default)
    {
        await context.QuitJourneys.AddAsync(journey, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(QuitJourney journey, CancellationToken cancellationToken = default)
    {
        // A tracked journey already has its added/removed smoked days detected by the change
        // tracker; calling Update() on it would mark new child rows as Modified instead of Added.
        if (context.Entry(journey).State == EntityState.Detached)
            context.QuitJourneys.Update(journey);

        await context.SaveChangesAsync(cancellationToken);
    }
}
