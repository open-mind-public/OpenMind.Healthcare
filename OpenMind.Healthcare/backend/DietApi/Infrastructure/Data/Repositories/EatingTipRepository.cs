using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Infrastructure.Data.Repositories;

public class EatingTipRepository(DietDbContext context) : IEatingTipRepository
{
    public async Task<IReadOnlyList<EatingTip>> GetAsync(
        TipCategory? category = null, CancellationToken cancellationToken = default)
    {
        var query = context.EatingTips.AsQueryable();

        if (category is not null)
            query = query.Where(t => t.Category == category);

        return await query.ToListAsync(cancellationToken);
    }
}
