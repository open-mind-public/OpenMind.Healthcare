using DDD.BuildingBlocks;
using DietApi.Domain.Entities;
using DietApi.Domain.Events;
using DietApi.Domain.Rules;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Aggregates;

/// <summary>
/// One calendar date under a plan, holding that date's food entries and deriving its own state
/// from them.
/// </summary>
/// <remarks>
/// <para>
/// This is its own aggregate root rather than a collection owned by <c>DietPlan</c>. Food entries
/// accrue three to six times a day, so a member's history reaches thousands of rows; loading and
/// re-saving all of it to record one breakfast would make every write scale with how long they
/// have been using the app. Nothing is lost by splitting, because no invariant spans two days.
/// </para>
/// <para>
/// Created lazily and destroyed when emptied: the first entry for a date creates the day, and
/// removing the last one leaves <see cref="IsEmpty"/> true so the repository deletes it. A day
/// never exists with zero entries, which is what keeps "not logged" and "logged nothing" from
/// becoming the same thing.
/// </para>
/// </remarks>
public class LoggedDay : AggregateRoot
{
    private readonly List<FoodEntry> _entries = [];

    public Guid DietPlanId { get; private set; }

    /// <summary>Denormalised so every query filters by owner without crossing an aggregate.</summary>
    public Guid UserId { get; private set; }

    public DateOnly Date { get; private set; }

    /// <summary>
    /// The targets in force when this day was first logged. Never updated - so lowering the
    /// plan's target tomorrow cannot flip a day that was on target into one that was not.
    /// </summary>
    public NutritionTargets TargetSnapshot { get; private set; } = null!;

    /// <summary>
    /// Recomputed from the entries on every change. Stored rather than derived on read so the
    /// calendar and statistics can read one small row per day instead of every entry.
    /// </summary>
    public NutritionValues Totals { get; private set; } = null!;

    /// <summary>
    /// Concurrency token, reassigned on every mutation. Two devices editing the same day would
    /// otherwise let one silently overwrite the other, leaving stored totals disagreeing with the
    /// entries beside them.
    /// </summary>
    public Guid Version { get; private set; }

    public IReadOnlyCollection<FoodEntry> Entries => _entries;

    public bool IsEmpty => _entries.Count == 0;

    private LoggedDay() { }

    public static LoggedDay StartDay(
        Guid dietPlanId,
        Guid userId,
        DateOnly date,
        NutritionTargets targetSnapshot,
        DateOnly planStartDate,
        DateTime? asOf = null)
    {
        var today = DateOnly.FromDateTime(asOf ?? DateTime.UtcNow);

        CheckRule(new EntryDateCannotBeInFutureRule(date, today));
        CheckRule(new EntryDateCannotPrecedePlanStartRule(date, planStartDate));

        return new LoggedDay
        {
            DietPlanId = dietPlanId,
            UserId = userId,
            Date = date,

            // A genuine copy, not the plan's own instance. Semantically this is the point of a
            // snapshot, and it is also required: an owned entity belongs to exactly one owner, so
            // sharing the plan's Targets instance would leave this day's columns unwritten.
            TargetSnapshot = NutritionTargets.Create(
                targetSnapshot.Calories, targetSnapshot.ProteinG, targetSnapshot.CarbsG, targetSnapshot.FatG),

            Totals = NutritionValues.Zero(),
            Version = Guid.NewGuid()
        };
    }

    public FoodEntry AddEntry(
        Guid foodLibraryItemId,
        Guid servingSizeId,
        string foodName,
        string servingLabel,
        decimal quantity,
        MealType mealType,
        NutritionValues nutritionPerServing,
        DateTime? asOf = null)
    {
        var now = asOf ?? DateTime.UtcNow;

        CheckRule(new QuantityMustBePositiveRule(quantity));
        CheckRule(new EntryCaloriesWithinCeilingRule(nutritionPerServing.Times(quantity).Calories));

        var entry = FoodEntry.Log(
            Id, foodLibraryItemId, servingSizeId, foodName, servingLabel,
            quantity, mealType, nutritionPerServing, now);

        _entries.Add(entry);
        Recalculate();

        Emit(new FoodEntryLoggedEvent(Id, Date, entry.Nutrition.Calories));
        return entry;
    }

    public FoodEntry UpdateEntry(
        Guid entryId,
        Guid servingSizeId,
        string servingLabel,
        decimal quantity,
        MealType mealType,
        NutritionValues nutritionPerServing)
    {
        var entry = _entries.SingleOrDefault(e => e.Id == entryId)
            ?? throw new DomainException("That food entry is not on this day");

        CheckRule(new QuantityMustBePositiveRule(quantity));
        CheckRule(new EntryCaloriesWithinCeilingRule(nutritionPerServing.Times(quantity).Calories));

        entry.Revise(servingSizeId, servingLabel, quantity, mealType, nutritionPerServing);
        Recalculate();

        return entry;
    }

    /// <returns><c>false</c> when the entry is not on this day.</returns>
    public bool RemoveEntry(Guid entryId)
    {
        var entry = _entries.SingleOrDefault(e => e.Id == entryId);
        if (entry is null)
            return false;

        _entries.Remove(entry);
        Recalculate();
        return true;
    }

    public DayAssessment Assess() =>
        DayAssessment.For(Date, Totals.Calories, TargetSnapshot.Calories, !IsEmpty);

    public IReadOnlyDictionary<MealType, IReadOnlyCollection<FoodEntry>> EntriesByMeal() =>
        _entries
            .GroupBy(e => e.MealType)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<FoodEntry>)[.. g.OrderBy(e => e.LoggedAt)]);

    /// <summary>
    /// Totals and the concurrency token move together on every mutation. Keeping them in one
    /// place is what makes the stored total safe to trust.
    /// </summary>
    private void Recalculate()
    {
        Totals = _entries.Aggregate(NutritionValues.Zero(), (running, entry) => running.Plus(entry.Nutrition));
        Version = Guid.NewGuid();
        SetUpdated();
    }
}
