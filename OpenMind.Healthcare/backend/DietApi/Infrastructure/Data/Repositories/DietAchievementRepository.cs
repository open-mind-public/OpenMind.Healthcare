using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Infrastructure.Data.Repositories;

public class DietAchievementRepository(DietDbContext context) : IDietAchievementRepository
{
    public async Task<IReadOnlyList<DietAchievement>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.DietAchievements.ToListAsync(cancellationToken);
}
