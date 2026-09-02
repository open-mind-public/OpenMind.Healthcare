using DDD.BuildingBlocks;
using DietApi.Domain.Entities;
using DietApi.Domain.Events;
using DietApi.Domain.Rules;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Aggregates;

/// <summary>
/// A member's standing intent: what they are aiming for, the details their target was sized
/// from, and the weight readings that track their progress. One active plan per member.
/// </summary>
/// <remarks>
/// Logged days are deliberately <em>not</em> owned by this aggregate. Food entries accrue three
/// to six times a day, so a member's history reaches thousands of rows and loading it to record
/// one breakfast would not scale. No invariant spans two days, so each day is its own aggregate.
/// </remarks>
public class DietPlan : AggregateRoot
{
    private readonly List<WeightReading> _weightReadings = [];
    private readonly List<UnlockedAchievement> _unlockedAchievements = [];

    public Guid UserId { get; private set; }
    public GoalType Goal { get; private set; }
    public DateOnly StartDate { get; private set; }
    public BodyMetrics BodyMetrics { get; private set; } = null!;
    public ActivityLevel ActivityLevel { get; private set; }
    public NutritionTargets Targets { get; private set; } = null!;
    public TargetSource TargetSource { get; private set; }
    public decimal? TargetWeightKg { get; private set; }

    /// <summary>
    /// Dated body weights, at most one per date. Part of the aggregate - mutable only through
    /// <see cref="RecordWeight"/> and <see cref="RemoveWeightReading"/>.
    /// </summary>
    public IReadOnlyCollection<WeightReading> WeightReadings => _weightReadings;

    /// <summary>
    /// Achievements this member has earned, with the date each was earned. Persisted rather than
    /// derived, so an achievement can never be taken back.
    /// </summary>
    public IReadOnlyCollection<UnlockedAchievement> UnlockedAchievements => _unlockedAchievements;

    private DietPlan() { }

    public static DietPlan Create(
        Guid userId,
        GoalType goal,
        DateOnly startDate,
        BodyMetrics bodyMetrics,
        ActivityLevel activityLevel,
        NutritionTargets targets,
        TargetSource targetSource,
        decimal currentWeightKg,
        decimal? targetWeightKg = null,
        DateTime? asOf = null)
    {
        var now = asOf ?? DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        CheckRule(new PlanStartDateCannotBeInFutureRule(startDate, today));
        CheckRule(new DailyCalorieTargetMustBePositiveRule(targets.Calories));
        CheckRule(new HeightMustBePlausibleRule(bodyMetrics.HeightCm));
        CheckRule(new AgeMustBePlausibleRule(bodyMetrics.Age));
        CheckRule(new WeightMustBePlausibleRule(currentWeightKg));
        CheckRule(new TargetWeightMustBePlausibleRule(targetWeightKg));

        if (userId == Guid.Empty)
            throw new DomainException("A diet plan must belong to a member");

        var plan = new DietPlan
        {
            UserId = userId,
            Goal = goal,
            StartDate = startDate,
            BodyMetrics = bodyMetrics,
            ActivityLevel = activityLevel,
            Targets = targets,
            TargetSource = targetSource,
            TargetWeightKg = targetWeightKg
        };

        // The weight supplied at setup becomes the first reading, so "current weight" has exactly
        // one source of truth from the moment the plan exists.
        plan._weightReadings.Add(WeightReading.Record(plan.Id, today, currentWeightKg, now));

        plan.Emit(new DietPlanCreatedEvent(plan.Id, userId, startDate));
        return plan;
    }

    /// <summary>
    /// Changes everything except the targets in force. A refreshed suggestion is offered to the
    /// member separately; it is never applied over a choice they made themselves.
    /// </summary>
    public void UpdatePlan(
        GoalType goal,
        DateOnly startDate,
        BodyMetrics bodyMetrics,
        ActivityLevel activityLevel,
        decimal? targetWeightKg = null,
        DateTime? asOf = null)
    {
        var today = DateOnly.FromDateTime(asOf ?? DateTime.UtcNow);

        CheckRule(new PlanStartDateCannotBeInFutureRule(startDate, today));
        CheckRule(new HeightMustBePlausibleRule(bodyMetrics.HeightCm));
        CheckRule(new AgeMustBePlausibleRule(bodyMetrics.Age));
        CheckRule(new TargetWeightMustBePlausibleRule(targetWeightKg));

        Goal = goal;
        StartDate = startDate;
        BodyMetrics = bodyMetrics;
        ActivityLevel = activityLevel;
        TargetWeightKg = targetWeightKg;
        SetUpdated();
    }

    /// <summary>The only way the targets in force change.</summary>
    public void SetTargets(NutritionTargets targets, TargetSource source)
    {
        CheckRule(new DailyCalorieTargetMustBePositiveRule(targets.Calories));

        Targets = targets;
        TargetSource = source;
        SetUpdated();
        Emit(new TargetsChangedEvent(Id, targets.Calories, source));
    }

    /// <summary>
    /// Records a weight for a date, replacing any reading already held for that date - a date
    /// never carries two.
    /// </summary>
    public WeightReading RecordWeight(DateOnly date, decimal weightKg, DateTime? asOf = null)
    {
        var now = asOf ?? DateTime.UtcNow;

        CheckRule(new WeightDateCannotBeInFutureRule(date, DateOnly.FromDateTime(now)));
        CheckRule(new WeightMustBePlausibleRule(weightKg));

        _weightReadings.RemoveAll(r => r.Date == date);

        var reading = WeightReading.Record(Id, date, weightKg, now);
        _weightReadings.Add(reading);

        SetUpdated();
        Emit(new WeightRecordedEvent(Id, date, weightKg));
        return reading;
    }

    /// <summary>
    /// Removes the reading for a date. Refuses when it is the only one the plan holds, because
    /// the target suggestion is calculated from current weight.
    /// </summary>
    /// <returns><c>false</c> when no reading exists for that date.</returns>
    public bool RemoveWeightReading(DateOnly date)
    {
        var reading = _weightReadings.SingleOrDefault(r => r.Date == date);
        if (reading is null)
            return false;

        CheckRule(new CannotRemoveLastWeightReadingRule(_weightReadings.Count));

        _weightReadings.Remove(reading);
        SetUpdated();
        return true;
    }

    /// <summary>
    /// Records that an achievement was earned. Silently does nothing when it already has been -
    /// so evaluating twice awards nothing the second time, and nothing is ever revoked.
    /// </summary>
    public void Unlock(Guid achievementId, DateOnly earnedOn)
    {
        if (_unlockedAchievements.Any(u => u.DietAchievementId == achievementId))
            return;

        _unlockedAchievements.Add(UnlockedAchievement.Earn(Id, achievementId, earnedOn));
        SetUpdated();
    }

    /// <summary>
    /// The member's weight over a period, with change since the plan started and distance left to
    /// their target weight.
    /// </summary>
    public WeightTrend WeightTrend(DateOnly? from = null, DateOnly? to = null, DateTime? asOf = null)
    {
        var today = DateOnly.FromDateTime(asOf ?? DateTime.UtcNow);
        var start = from ?? StartDate;
        var end = to ?? today;

        var inPeriod = _weightReadings
            .Where(r => r.Date >= start && r.Date <= end)
            .OrderBy(r => r.Date)
            .ToList();

        // Change is measured from the reading nearest the plan's start, not from the first one in
        // the period being viewed - otherwise scrolling the chart would change how much progress
        // the member appears to have made.
        var baseline = _weightReadings
            .OrderBy(r => Math.Abs(r.Date.DayNumber - StartDate.DayNumber))
            .ThenBy(r => r.Date)
            .FirstOrDefault();

        var current = _weightReadings
            .Where(r => r.Date <= today)
            .OrderByDescending(r => r.Date)
            .FirstOrDefault()
            ?? _weightReadings.OrderBy(r => r.Date).FirstOrDefault();

        return ValueObjects.WeightTrend.Create(
            inPeriod, baseline?.WeightKg, current?.WeightKg, TargetWeightKg, Goal);
    }

    /// <summary>
    /// The most recent reading at or before <paramref name="asOf"/>. Never null: setup stores the
    /// supplied weight as the first reading, and the last one cannot be deleted.
    /// </summary>
    public decimal CurrentWeightKg(DateTime? asOf = null)
    {
        var today = DateOnly.FromDateTime(asOf ?? DateTime.UtcNow);

        var reading = _weightReadings
            .Where(r => r.Date <= today)
            .OrderByDescending(r => r.Date)
            .FirstOrDefault()
            ?? _weightReadings.OrderBy(r => r.Date).FirstOrDefault();

        return reading?.WeightKg
            ?? throw new DomainException("This plan has no weight reading");
    }
}
