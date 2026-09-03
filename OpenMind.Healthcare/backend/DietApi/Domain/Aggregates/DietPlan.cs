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
    private readonly List<ExerciseShortcut> _exerciseShortcuts = [];

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

    /// <summary>
    /// Saved ways to record an exercise session in one tap, in the member's chosen order.
    /// </summary>
    /// <remarks>
    /// Owned by the plan rather than being its own aggregate, and deliberately so. Two rules span
    /// the whole set - at most <see cref="MaxShortcuts"/> of them, and no two recording the same
    /// activity for the same duration. Neither can be enforced from inside a single shortcut
    /// without a read-modify-write race: two concurrent saves would both pass the check. An
    /// invariant over a set needs a consistency boundary containing the set, which is what an
    /// aggregate is for.
    /// <para>
    /// The usual objection to owning a collection - that writes come to scale with its size - does
    /// not apply here, because the cap bounds it at ten. That cap is a requirement rather than a
    /// convenience, and it is what makes ownership safe.
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<ExerciseShortcut> ExerciseShortcuts => _exerciseShortcuts;

    /// <summary>Past roughly this many, scanning the list costs more than typing the session.</summary>
    public const int MaxShortcuts = 10;

    public int RemainingShortcutSlots => Math.Max(0, MaxShortcuts - _exerciseShortcuts.Count);

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

    // --- Exercise shortcuts ------------------------------------------------

    /// <summary>Ordered as the member arranged them.</summary>
    public IReadOnlyList<ExerciseShortcut> ShortcutsInOrder() =>
        [.. _exerciseShortcuts.OrderBy(s => s.Position)];

    public ExerciseShortcut? ExerciseShortcut(Guid shortcutId) =>
        _exerciseShortcuts.SingleOrDefault(s => s.Id == shortcutId);

    /// <summary>
    /// Saves a new shortcut at the end of the list.
    /// </summary>
    /// <remarks>
    /// The duration rules are the very same objects that guard a recorded session, not copies of
    /// them. That is what makes "a duration refused on a session is refused on a shortcut" true by
    /// construction rather than by coincidence - a shortcut cannot hold a session that could never
    /// be recorded.
    /// </remarks>
    public ExerciseShortcut SaveExerciseShortcut(
        Guid activityTypeId, int durationMinutes, string name, DateTime? asOf = null)
    {
        CheckRule(new DurationMustBePositiveRule(durationMinutes));
        CheckRule(new DurationWithinCeilingRule(durationMinutes));
        CheckRule(new ShortcutNameMustNotBeEmptyRule(name));
        CheckRule(new ShortcutNameWithinLengthRule(name));
        CheckRule(new ShortcutLimitRule(_exerciseShortcuts.Count, MaxShortcuts));

        var duplicate = _exerciseShortcuts.FirstOrDefault(s => s.Records(activityTypeId, durationMinutes));
        CheckRule(new ShortcutMustBeUniqueRule(duplicate?.Name));

        var shortcut = Entities.ExerciseShortcut.Save(
            Id, activityTypeId, name, durationMinutes, _exerciseShortcuts.Count, asOf ?? DateTime.UtcNow);

        _exerciseShortcuts.Add(shortcut);
        Normalise();
        SetUpdated();

        return shortcut;
    }

    /// <returns><c>false</c> when the shortcut is not this member's.</returns>
    public bool RenameExerciseShortcut(Guid shortcutId, string name)
    {
        var shortcut = ExerciseShortcut(shortcutId);
        if (shortcut is null)
            return false;

        CheckRule(new ShortcutNameMustNotBeEmptyRule(name));
        CheckRule(new ShortcutNameWithinLengthRule(name));

        // No uniqueness check: duplicates compare activity and duration, so renaming cannot create
        // one.
        shortcut.Rename(name);
        SetUpdated();

        return true;
    }

    /// <summary>
    /// Rearranges the shortcuts into the given order.
    /// </summary>
    /// <remarks>
    /// Takes the complete ordered list rather than a "move this one up", because a full list is
    /// idempotent and has no race: two clients sending different orders produce one of the two,
    /// never an interleaving.
    /// </remarks>
    public void ReorderExerciseShortcuts(IReadOnlyList<Guid> orderedIds)
    {
        CheckRule(new ReorderMustCoverEveryShortcutRule(
            orderedIds, [.. _exerciseShortcuts.Select(s => s.Id)]));

        for (var position = 0; position < orderedIds.Count; position++)
        {
            ExerciseShortcut(orderedIds[position])!.MoveTo(position);
        }

        SetUpdated();
    }

    /// <returns><c>false</c> when the shortcut is not this member's.</returns>
    public bool RemoveExerciseShortcut(Guid shortcutId)
    {
        var shortcut = ExerciseShortcut(shortcutId);
        if (shortcut is null)
            return false;

        _exerciseShortcuts.Remove(shortcut);
        Normalise();
        SetUpdated();

        return true;
    }

    /// <summary>
    /// Rewrites positions contiguously from zero, so a removal never leaves a hole and no two
    /// shortcuts can share a position.
    /// </summary>
    private void Normalise()
    {
        var position = 0;
        foreach (var shortcut in _exerciseShortcuts.OrderBy(s => s.Position).ToList())
        {
            shortcut.MoveTo(position++);
        }
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
