using DietApi.Domain.Aggregates;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.TestSupport;

/// <summary>
/// Builds a <see cref="DietPlan"/> for a test around a single pinned moment.
/// Tests pass <see cref="Clock"/> back in as the "as of" argument so results never depend on how
/// long the test itself takes, or on when in the day it runs.
/// </summary>
public sealed class DietPlanBuilder
{
    private readonly DateTime _clock = DateTime.UtcNow;

    private Guid _userId = Guid.NewGuid();
    private GoalType _goal = GoalType.LoseWeight;
    private int _startedDaysAgo = 30;
    private decimal _heightCm = 178m;
    private int _age = 34;
    private BiologicalSex _sex = BiologicalSex.Male;
    private ActivityLevel _activityLevel = ActivityLevel.ModeratelyActive;
    private NutritionTargets _targets = NutritionTargets.Create(2100, 157.5m, 210m, 70m);
    private TargetSource _targetSource = TargetSource.Suggested;
    private decimal _currentWeightKg = 84.6m;
    private decimal? _targetWeightKg = 78m;

    private readonly List<(DateOnly Date, decimal WeightKg)> _extraReadings = [];
    private readonly List<(Guid ActivityTypeId, int Minutes, string Name)> _shortcuts = [];

    public static DietPlanBuilder APlan() => new();

    /// <summary>The moment the plan is measured at. Pass this as "as of" when asserting.</summary>
    public DateTime Clock => _clock;

    public DateOnly Today => DateOnly.FromDateTime(_clock);

    public DateOnly DaysAgo(int days) => Today.AddDays(-days);

    public Guid UserId => _userId;

    public DietPlanBuilder ForUser(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public DietPlanBuilder WithGoal(GoalType goal)
    {
        _goal = goal;
        return this;
    }

    public DietPlanBuilder StartedDaysAgo(int days)
    {
        _startedDaysAgo = days;
        return this;
    }

    public DietPlanBuilder WithBody(decimal heightCm = 178m, int age = 34, BiologicalSex sex = BiologicalSex.Male)
    {
        _heightCm = heightCm;
        _age = age;
        _sex = sex;
        return this;
    }

    public DietPlanBuilder WithActivity(ActivityLevel level)
    {
        _activityLevel = level;
        return this;
    }

    public DietPlanBuilder WithTargets(int calories, TargetSource source = TargetSource.Suggested)
    {
        _targets = NutritionTargets.Create(calories);
        _targetSource = source;
        return this;
    }

    public DietPlanBuilder Weighing(decimal currentWeightKg)
    {
        _currentWeightKg = currentWeightKg;
        return this;
    }

    public DietPlanBuilder TargetingWeight(decimal? targetWeightKg)
    {
        _targetWeightKg = targetWeightKg;
        return this;
    }

    /// <summary>A shortcut the member has already saved.</summary>
    public DietPlanBuilder WithShortcut(Guid activityTypeId, int minutes, string name)
    {
        _shortcuts.Add((activityTypeId, minutes, name));
        return this;
    }

    public DietPlanBuilder WeighedDaysAgo(int daysAgo, decimal weightKg)
    {
        _extraReadings.Add((DaysAgo(daysAgo), weightKg));
        return this;
    }

    public DietPlan Build()
    {
        var plan = DietPlan.Create(
            _userId,
            _goal,
            DaysAgo(_startedDaysAgo),
            BodyMetrics.Create(_heightCm, _age, _sex),
            _activityLevel,
            _targets,
            _targetSource,
            _currentWeightKg,
            _targetWeightKg,
            _clock);

        foreach (var reading in _extraReadings)
        {
            plan.RecordWeight(reading.Date, reading.WeightKg, _clock);
        }

        foreach (var shortcut in _shortcuts)
        {
            plan.SaveExerciseShortcut(shortcut.ActivityTypeId, shortcut.Minutes, shortcut.Name, _clock);
        }

        return plan;
    }
}
