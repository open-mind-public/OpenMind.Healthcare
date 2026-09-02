using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for the <see cref="DietPlan"/> aggregate root.
/// </summary>
public class DietPlanRepository(DietDbContext context) : IDietPlanRepository
{
    public async Task<DietPlan?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.DietPlans
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(DietPlan plan, CancellationToken cancellationToken = default)
    {
        await context.DietPlans.AddAsync(plan, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DietPlan plan, CancellationToken cancellationToken = default)
    {
        // A tracked plan already has its added/removed weight readings detected by the change
        // tracker; calling Update() on it would mark new child rows as Modified instead of Added.
        if (context.Entry(plan).State == EntityState.Detached)
            context.DietPlans.Update(plan);

        await context.SaveChangesAsync(cancellationToken);
    }
}
