using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Repositories;

/// <summary>One logged day's totals, and the target that was in force on it.</summary>
/// <remarks>
/// The target travels with the day rather than being read from the plan. That is what lets a
/// period spanning a target change be judged against what was actually in force each day (FR-011).
/// Macronutrient grams are <c>decimal</c> and are summed in memory, never in SQL (ADR 0002).
/// The macronutrient <em>targets</em> are nullable because a plan may carry a calorie target and
/// no macronutrient ones - which is the case FR-012 requires be presented without inventing a
/// comparison.
/// </remarks>
public record DayIntakeRow(
    DateOnly Date,
    int Calories,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG,
    int TargetCalories,
    decimal? TargetProteinG,
    decimal? TargetCarbsG,
    decimal? TargetFatG);

/// <summary>Energy attributed to one meal across a period.</summary>
public record MealIntakeRow(MealType Meal, int Kilocalories, int EntryCount);

/// <summary>One food's contribution across a period.</summary>
public record FoodContributionRow(Guid FoodLibraryItemId, string FoodName, int Kilocalories, int Times);

/// <summary>Energy attributed to one food category across a period.</summary>
public record CategoryIntakeRow(FoodCategory Category, int Kilocalories);

/// <summary>
/// Energy logged in one quarter-hour of the UTC day.
/// </summary>
/// <remarks>
/// Quarter-hour rather than hourly so the caller's offset can be applied by rotation and still be
/// exact at +05:30 and +05:45. Rotating hourly buckets is only correct for whole-hour offsets.
/// </remarks>
public record QuarterHourRow(int Hour, int Quarter, int Kilocalories);

/// <summary>
/// The read model behind diet analytics.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not an aggregate repository. It returns flat records with no behaviour, does the
/// arithmetic the database is good at, and makes no judgements - every one of those stays in the
/// domain, where it is testable without a database.
/// </para>
/// <para>
/// This is the one place the diet service reads its own data through something other than an
/// aggregate repository, and it is a deliberate departure recorded in the feature plan's
/// Complexity Tracking. Asking <c>ILoggedDayRepository</c> to do this as well would give one
/// interface two unrelated jobs and invite someone to answer a reporting question by loading a
/// thousand aggregates.
/// </para>
/// <para>
/// Every method filters by <c>userId</c>, so another member's data is unreachable rather than
/// merely forbidden.
/// </para>
/// </remarks>
public interface IDietAnalyticsRepository
{
    /// <summary>
    /// One row per <em>logged</em> day in the range. Days with nothing logged are absent, because
    /// they are absences rather than zero-calorie days.
    /// </summary>
    Task<IReadOnlyList<DayIntakeRow>> GetDayRowsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealIntakeRow>> GetMealRowsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>Ordered by energy, largest first, capped at <paramref name="limit"/>.</summary>
    Task<IReadOnlyList<FoodContributionRow>> GetTopFoodRowsAsync(
        Guid userId, DateOnly from, DateOnly to, int limit = 10, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryIntakeRow>> GetCategoryRowsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuarterHourRow>> GetQuarterHourRowsAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
