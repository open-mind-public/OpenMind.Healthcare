using DietApi.Domain.Aggregates;
using DietApi.Domain.Entities;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.TestSupport;

/// <summary>
/// Builds a <see cref="LoggedDay"/> around a pinned moment, so assertions never depend on when
/// the test runs.
/// </summary>
public sealed class LoggedDayBuilder
{
    private readonly DateTime _clock = DateTime.UtcNow;

    private Guid _planId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private int _daysAgo;
    private int _targetCalories = 2100;
    private int _planStartedDaysAgo = 30;

    private readonly List<(FoodLibraryItem Food, int ServingIndex, decimal Quantity, MealType Meal)> _entries = [];

    public static LoggedDayBuilder ADay() => new();

    public DateTime Clock => _clock;
    public DateOnly Today => DateOnly.FromDateTime(_clock);
    public DateOnly Date => Today.AddDays(-_daysAgo);
    public DateOnly PlanStartDate => Today.AddDays(-_planStartedDaysAgo);
    public Guid UserId => _userId;
    public Guid PlanId => _planId;
    public NutritionTargets Targets => NutritionTargets.Create(_targetCalories);

    public LoggedDayBuilder ForUser(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public LoggedDayBuilder ForPlan(Guid planId)
    {
        _planId = planId;
        return this;
    }

    public LoggedDayBuilder DaysAgo(int days)
    {
        _daysAgo = days;
        return this;
    }

    public LoggedDayBuilder Targeting(int calories)
    {
        _targetCalories = calories;
        return this;
    }

    public LoggedDayBuilder PlanStartedDaysAgo(int days)
    {
        _planStartedDaysAgo = days;
        return this;
    }

    public LoggedDayBuilder Ate(
        FoodLibraryItem food,
        decimal quantity = 1m,
        MealType meal = MealType.Breakfast,
        int servingIndex = 0)
    {
        _entries.Add((food, servingIndex, quantity, meal));
        return this;
    }

    public LoggedDay Build()
    {
        var day = LoggedDay.StartDay(_planId, _userId, Date, Targets, PlanStartDate, _clock);

        foreach (var entry in _entries)
        {
            var serving = entry.Food.ServingSizes.ElementAt(entry.ServingIndex);
            day.AddEntry(
                entry.Food.Id, serving.Id, entry.Food.Name, serving.Label,
                entry.Quantity, entry.Meal, serving.Nutrition, _clock);
        }

        return day;
    }
}
