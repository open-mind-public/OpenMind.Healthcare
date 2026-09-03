using DietApi.Domain.Aggregates;

namespace DietApi.Tests.TestSupport;

/// <summary>
/// Builds an <see cref="ExerciseDay"/> around a pinned moment, so assertions never depend on when
/// the test runs.
/// </summary>
public sealed class ExerciseDayBuilder
{
    private readonly DateTime _clock = DateTime.UtcNow;

    private Guid _planId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private int _daysAgo;
    private int _planStartedDaysAgo = 30;
    private decimal _weightKg = 70m;

    private readonly List<(ActivityType Activity, int DurationMinutes)> _sessions = [];

    public static ExerciseDayBuilder ADay() => new();

    public DateTime Clock => _clock;
    public DateOnly Today => DateOnly.FromDateTime(_clock);
    public DateOnly Date => Today.AddDays(-_daysAgo);
    public DateOnly PlanStartDate => Today.AddDays(-_planStartedDaysAgo);
    public Guid UserId => _userId;
    public Guid PlanId => _planId;
    public decimal WeightKg => _weightKg;

    public ExerciseDayBuilder ForUser(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public ExerciseDayBuilder ForPlan(Guid planId)
    {
        _planId = planId;
        return this;
    }

    public ExerciseDayBuilder DaysAgo(int days)
    {
        _daysAgo = days;
        return this;
    }

    public ExerciseDayBuilder PlanStartedDaysAgo(int days)
    {
        _planStartedDaysAgo = days;
        return this;
    }

    public ExerciseDayBuilder Weighing(decimal weightKg)
    {
        _weightKg = weightKg;
        return this;
    }

    public ExerciseDayBuilder Did(ActivityType activity, int durationMinutes)
    {
        _sessions.Add((activity, durationMinutes));
        return this;
    }

    public ExerciseDay Build()
    {
        var day = ExerciseDay.StartDay(_planId, _userId, Date, PlanStartDate, _clock);

        foreach (var session in _sessions)
        {
            day.AddEntry(
                session.Activity.Id,
                session.Activity.Name,
                session.Activity.Met,
                session.DurationMinutes,
                _weightKg,
                _clock);
        }

        return day;
    }
}
