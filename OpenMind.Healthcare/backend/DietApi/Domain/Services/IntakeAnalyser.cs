using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Services;

/// <summary>
/// Turns read-model rows into the intake figures a member sees.
/// </summary>
/// <remarks>
/// Every judgement in this feature that concerns "where did the calories go" lives here: which
/// denominator an average uses, how a day is assessed, how shares are rounded so they still add
/// up. The repository does the summing; this decides what the sums mean.
/// <para>
/// Pure — no clock, no repository, no dependency — so all of it is testable without a database.
/// </para>
/// </remarks>
public class IntakeAnalyser
{
    /// <summary>
    /// The period's totals and day-state split.
    /// </summary>
    /// <param name="days">One row per logged day. Days absent from this list were not logged.</param>
    /// <param name="totalDays">Calendar days in the period, logged or not.</param>
    /// <param name="previousDays">The comparison window's rows, or null when there is no comparison.</param>
    public IntakeSummary Summarise(
        IReadOnlyList<DayIntakeRow> days,
        int totalDays,
        IReadOnlyList<DayIntakeRow>? previousDays = null)
    {
        var onTarget = 0;
        var overTarget = 0;

        foreach (var day in days)
        {
            // The same assessment the rest of the programme uses, so a day cannot read one way on
            // the calendar and another in analytics.
            var state = DayAssessment.For(day.Date, day.Calories, day.TargetCalories, hasEntries: true).State;

            if (state == DayState.OnTarget)
                onTarget++;
            else if (state == DayState.OverTarget)
                overTarget++;
        }

        return IntakeSummary.Create(
            totalKilocalories: days.Sum(d => d.Calories),
            loggedDays: days.Count,
            totalDays: totalDays,
            onTargetDays: onTarget,
            overTargetDays: overTarget,
            previousAverageDailyKilocalories: AverageOrNull(previousDays));
    }

    public MealBreakdown BreakDownByMeal(IReadOnlyList<MealIntakeRow> rows)
    {
        // Exhaustive: every meal appears, at zero if nothing was logged, so the parts sum.
        var byMeal = rows.ToDictionary(r => r.Meal);
        var meals = Enum.GetValues<MealType>();

        var energies = meals.Select(m => byMeal.TryGetValue(m, out var row) ? row.Kilocalories : 0).ToList();
        var shares = PercentageShares.Of(energies, energies.Sum());

        return MealBreakdown.Create(
        [
            .. meals.Select((meal, i) => new MealShare(
                meal,
                energies[i],
                shares[i],
                byMeal.TryGetValue(meal, out var row) ? row.EntryCount : 0))
        ]);
    }

    public CategoryBreakdown BreakDownByCategory(IReadOnlyList<CategoryIntakeRow> rows)
    {
        var byCategory = rows.ToDictionary(r => r.Category);
        var categories = Enum.GetValues<FoodCategory>();

        var energies = categories
            .Select(c => byCategory.TryGetValue(c, out var row) ? row.Kilocalories : 0)
            .ToList();

        var shares = PercentageShares.Of(energies, energies.Sum());

        return CategoryBreakdown.Create(
        [
            .. categories.Select((category, i) => new CategoryShare(category, energies[i], shares[i]))
        ]);
    }

    /// <summary>
    /// The top contributing foods, with each one's share of the period's <em>whole</em> intake.
    /// </summary>
    /// <remarks>
    /// Shares are of the period total, not of the top ten, so "18% of everything you logged" means
    /// what it says. They therefore do not sum to 100, which is correct and intended.
    /// </remarks>
    public IReadOnlyList<FoodContribution> TopFoods(IReadOnlyList<FoodContributionRow> rows, int periodTotalKilocalories)
    {
        if (rows.Count == 0)
            return [];

        return
        [
            .. rows.Select(r => new FoodContribution(
                r.FoodLibraryItemId,
                r.FoodName,
                r.Kilocalories,
                periodTotalKilocalories <= 0
                    ? 0m
                    : Math.Round(r.Kilocalories * 100m / periodTotalKilocalories, 1, MidpointRounding.AwayFromZero),
                r.Times))
        ];
    }

    private static int? AverageOrNull(IReadOnlyList<DayIntakeRow>? days)
    {
        if (days is null || days.Count == 0)
            return null;

        return (int)Math.Round((decimal)days.Sum(d => d.Calories) / days.Count, MidpointRounding.AwayFromZero);
    }
}
