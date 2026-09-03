using DietApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Infrastructure.Data.Repositories;

/// <summary>
/// The read model behind diet analytics: grouped queries, no aggregates, no writes.
/// </summary>
/// <remarks>
/// <para>
/// Every method here projects in SQL and returns records with no behaviour. Food entries are never
/// materialised to be totalled - a member with three years of history has roughly 4,400 of them,
/// and loading those to answer "which foods contributed most" would fail the performance criterion
/// for no benefit anyone can see.
/// </para>
/// <para>
/// Only integers are aggregated in SQL. Macronutrient grams travel out as <c>decimal</c> per day
/// and are summed in the domain, because EF Core maps <c>decimal</c> to SQLite <c>TEXT</c>
/// (ADR 0002). A probe showed <c>SUM</c> over such a column returning a <em>correct</em> answer on
/// small data, which makes it a trap rather than a safeguard: it would pass every test written
/// over a handful of days and drift later.
/// </para>
/// </remarks>
public class DietAnalyticsRepository(DietDbContext context) : IDietAnalyticsRepository
{
    public async Task<IReadOnlyList<DayIntakeRow>> GetDayRowsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        // One small row per logged day. The target comes from the day's own stored snapshot, not
        // from the plan, which is what makes a period spanning a target change judgeable (FR-011).
        return await context.LoggedDays
            .Where(d => d.UserId == userId && d.Date >= from && d.Date <= to && d.Entries.Any())
            .OrderBy(d => d.Date)
            .Select(d => new DayIntakeRow(
                d.Date,
                d.Totals.Calories,
                d.Totals.ProteinG,
                d.Totals.CarbsG,
                d.Totals.FatG,
                d.TargetSnapshot.Calories,
                d.TargetSnapshot.ProteinG,
                d.TargetSnapshot.CarbsG,
                d.TargetSnapshot.FatG))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MealIntakeRow>> GetMealRowsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await Entries(userId, from, to)
            .GroupBy(e => e.MealType)
            .Select(g => new MealIntakeRow(g.Key, g.Sum(e => e.Nutrition.Calories), g.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FoodContributionRow>> GetTopFoodRowsAsync(
        Guid userId, DateOnly from, DateOnly to, int limit = 10, CancellationToken cancellationToken = default)
    {
        // Ordered by summed calories, which is an int column. Never by a decimal - ADR 0002 warns
        // that text columns sort lexicographically.
        //
        // The ordering and the cap must come BEFORE the projection. Ordering by a property of the
        // projected record instead - .Select(...).OrderByDescending(r => r.Kilocalories) - reads
        // more naturally and does not translate at all; EF Core throws at runtime, which the
        // in-memory fake cannot reproduce and only the scale test catches.
        return await Entries(userId, from, to)
            .GroupBy(e => new { e.FoodLibraryItemId, e.FoodName })
            .OrderByDescending(g => g.Sum(e => e.Nutrition.Calories))
            .ThenBy(g => g.Key.FoodName)
            .Take(Math.Clamp(limit, 1, 50))
            .Select(g => new FoodContributionRow(
                g.Key.FoodLibraryItemId,
                g.Key.FoodName,
                g.Sum(e => e.Nutrition.Calories),
                g.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryIntakeRow>> GetCategoryRowsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        // Category is not snapshotted onto the entry, so it comes from the library. That is
        // deliberate: a category is a classification rather than a figure the member acted on, so
        // reclassifying a food should reclassify it in past periods too.
        return await Entries(userId, from, to)
            .Join(context.FoodLibraryItems,
                entry => entry.FoodLibraryItemId,
                food => food.Id,
                (entry, food) => new { food.Category, entry.Nutrition.Calories })
            .GroupBy(x => x.Category)
            .Select(g => new CategoryIntakeRow(g.Key, g.Sum(x => x.Calories)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QuarterHourRow>> GetQuarterHourRowsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        // Ninety-six buckets in UTC. The caller's offset is applied by rotating them in the
        // domain, and quarter-hour resolution is what makes that exact at +05:30 and +05:45.
        return await Entries(userId, from, to)
            .GroupBy(e => new { e.LoggedAt.Hour, Quarter = e.LoggedAt.Minute / 15 })
            .Select(g => new QuarterHourRow(g.Key.Hour, g.Key.Quarter, g.Sum(e => e.Nutrition.Calories)))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Every entry on the member's logged days in the range, as a queryable the database resolves.
    /// </summary>
    /// <remarks>
    /// <c>FoodEntry</c> is an owned collection with no <c>DbSet</c> of its own, so this is the only
    /// way in. EF Core translates it to a join onto <c>FoodEntries</c>; nothing is enumerated here.
    /// </remarks>
    private IQueryable<Domain.Entities.FoodEntry> Entries(Guid userId, DateOnly from, DateOnly to) =>
        context.LoggedDays
            .Where(d => d.UserId == userId && d.Date >= from && d.Date <= to)
            .SelectMany(d => d.Entries);
}
