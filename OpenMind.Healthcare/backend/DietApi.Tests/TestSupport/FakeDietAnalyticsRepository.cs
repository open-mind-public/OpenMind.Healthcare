using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.TestSupport;

/// <summary>
/// In-memory stand-in for the analytics read model, built from days a test describes.
/// </summary>
/// <remarks>
/// It performs the same grouping the real repository asks SQL for, so a handler test exercises the
/// same shapes. What it does <em>not</em> do is reproduce SQLite's behaviour — the properties that
/// depend on that (a decimal never summed in SQL, a join for category) are asserted against a real
/// database in the infrastructure tests instead.
/// </remarks>
public sealed class FakeDietAnalyticsRepository : IDietAnalyticsRepository
{
    /// <summary>One entry on a seeded day.</summary>
    public sealed record SeededEntry(
        string FoodName,
        FoodCategory Category,
        MealType Meal,
        int Calories,
        DateTime LoggedAt,
        Guid FoodLibraryItemId);

    /// <summary>One day a test wants the member to have logged.</summary>
    public sealed record SeededDay(
        DateOnly Date,
        IReadOnlyList<SeededEntry> Entries,
        decimal ProteinG,
        decimal CarbsG,
        decimal FatG,
        int TargetCalories,
        decimal? TargetProteinG,
        decimal? TargetCarbsG,
        decimal? TargetFatG)
    {
        public int Calories => Entries.Sum(e => e.Calories);
    }

    private readonly Guid _userId;
    private readonly List<SeededDay> _days = [];

    private FakeDietAnalyticsRepository(Guid userId) => _userId = userId;

    public static FakeDietAnalyticsRepository For(Guid userId) => new(userId);

    /// <summary>Adds a day. Macronutrients and targets default to a plausible plan.</summary>
    public FakeDietAnalyticsRepository WithDay(
        DateOnly date,
        IEnumerable<SeededEntry> entries,
        decimal proteinG = 100m,
        decimal carbsG = 200m,
        decimal fatG = 70m,
        int targetCalories = 2100,
        decimal? targetProteinG = 157.5m,
        decimal? targetCarbsG = 210m,
        decimal? targetFatG = 70m)
    {
        _days.Add(new SeededDay(
            date, [.. entries], proteinG, carbsG, fatG,
            targetCalories, targetProteinG, targetCarbsG, targetFatG));
        return this;
    }

    /// <summary>A day of one meal, for tests that care about the total and not the shape.</summary>
    public FakeDietAnalyticsRepository WithSimpleDay(
        DateOnly date, int calories, MealType meal = MealType.Dinner, DateTime? loggedAt = null,
        int targetCalories = 2100, decimal? targetProteinG = 157.5m)
    {
        var at = loggedAt ?? date.ToDateTime(new TimeOnly(19, 0));
        return WithDay(
            date,
            [new SeededEntry("Seeded meal", FoodCategory.PreparedMeal, meal, calories, at, Guid.NewGuid())],
            targetCalories: targetCalories,
            targetProteinG: targetProteinG);
    }

    public IReadOnlyList<SeededDay> Days => _days;

    public Task<IReadOnlyList<DayIntakeRow>> GetDayRowsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DayIntakeRow> rows =
        [
            .. Scope(userId, from, to)
                .OrderBy(d => d.Date)
                .Select(d => new DayIntakeRow(
                    d.Date, d.Calories, d.ProteinG, d.CarbsG, d.FatG,
                    d.TargetCalories, d.TargetProteinG, d.TargetCarbsG, d.TargetFatG))
        ];

        return Task.FromResult(rows);
    }

    public Task<IReadOnlyList<MealIntakeRow>> GetMealRowsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MealIntakeRow> rows =
        [
            .. Entries(userId, from, to)
                .GroupBy(e => e.Meal)
                .Select(g => new MealIntakeRow(g.Key, g.Sum(e => e.Calories), g.Count()))
        ];

        return Task.FromResult(rows);
    }

    public Task<IReadOnlyList<FoodContributionRow>> GetTopFoodRowsAsync(
        Guid userId, DateOnly from, DateOnly to, int limit = 10, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FoodContributionRow> rows =
        [
            .. Entries(userId, from, to)
                .GroupBy(e => new { e.FoodLibraryItemId, e.FoodName })
                .Select(g => new FoodContributionRow(
                    g.Key.FoodLibraryItemId, g.Key.FoodName, g.Sum(e => e.Calories), g.Count()))
                .OrderByDescending(r => r.Kilocalories)
                .ThenBy(r => r.FoodName)
                .Take(Math.Clamp(limit, 1, 50))
        ];

        return Task.FromResult(rows);
    }

    public Task<IReadOnlyList<CategoryIntakeRow>> GetCategoryRowsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CategoryIntakeRow> rows =
        [
            .. Entries(userId, from, to)
                .GroupBy(e => e.Category)
                .Select(g => new CategoryIntakeRow(g.Key, g.Sum(e => e.Calories)))
        ];

        return Task.FromResult(rows);
    }

    public Task<IReadOnlyList<QuarterHourRow>> GetQuarterHourRowsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QuarterHourRow> rows =
        [
            .. Entries(userId, from, to)
                .GroupBy(e => new { e.LoggedAt.Hour, Quarter = e.LoggedAt.Minute / 15 })
                .Select(g => new QuarterHourRow(g.Key.Hour, g.Key.Quarter, g.Sum(e => e.Calories)))
        ];

        return Task.FromResult(rows);
    }

    /// <summary>Another member sees nothing, exactly as the real queries arrange.</summary>
    private IEnumerable<SeededDay> Scope(Guid userId, DateOnly from, DateOnly to) =>
        userId != _userId ? [] : _days.Where(d => d.Date >= from && d.Date <= to);

    private IEnumerable<SeededEntry> Entries(Guid userId, DateOnly from, DateOnly to) =>
        Scope(userId, from, to).SelectMany(d => d.Entries);
}
