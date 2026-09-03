using DDD.BuildingBlocks;
using DietApi.Domain.Entities;
using DietApi.Domain.Events;
using DietApi.Domain.Rules;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Aggregates;

/// <summary>
/// One calendar date's recorded activity under a plan.
/// </summary>
/// <remarks>
/// <para>
/// This is its own aggregate root, and deliberately <em>not</em> part of <c>LoggedDay</c>.
/// A logged day is created by the first food entry and destroyed when the last one is removed, so
/// exercise living inside it would vanish when a member deleted their dinner, and would have
/// nowhere to live on a day with a run and no food logged. FR-013 forbids both. The two are
/// independent per-day aggregates that happen to share a date; neither can create or destroy the
/// other (research.md R-002).
/// </para>
/// <para>
/// It mirrors that lifecycle all the same: created lazily by the first session, and left
/// <see cref="IsEmpty"/> when the last is removed so the repository deletes it. A date never
/// carries an exercise day with no sessions on it, which is what keeps "did nothing" and
/// "recorded nothing" from becoming the same thing.
/// </para>
/// <para>
/// Nothing here reads or writes a logged day, a target or a day's assessment. That absence is the
/// feature's central guarantee, not an oversight.
/// </para>
/// </remarks>
public class ExerciseDay : AggregateRoot
{
    private static readonly EnergyEstimator Estimator = new();

    private readonly List<ExerciseEntry> _entries = [];

    public Guid DietPlanId { get; private set; }

    /// <summary>Denormalised so every query filters by owner without crossing an aggregate.</summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The calendar day. Fixed when the day is created - a session is moved by being deleted and
    /// re-recorded, not by editing its date (FR-007).
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Recomputed from the entries on every change. Stored rather than derived on read so the
    /// calendar and the weekly summary can read one small row per day instead of every session.
    /// </summary>
    public ExerciseTotals Totals { get; private set; } = null!;

    /// <summary>
    /// Concurrency token, reassigned on every mutation. Two devices editing the same day would
    /// otherwise let one silently overwrite the other, leaving stored totals disagreeing with the
    /// sessions beside them.
    /// </summary>
    public Guid Version { get; private set; }

    public IReadOnlyCollection<ExerciseEntry> Entries => _entries;

    public bool IsEmpty => _entries.Count == 0;

    private ExerciseDay() { }

    public static ExerciseDay StartDay(
        Guid dietPlanId,
        Guid userId,
        DateOnly date,
        DateOnly planStartDate,
        DateTime? asOf = null)
    {
        var today = DateOnly.FromDateTime(asOf ?? DateTime.UtcNow);

        CheckRule(new ExerciseDateCannotBeInFutureRule(date, today));
        CheckRule(new ExerciseDateCannotPrecedePlanStartRule(date, planStartDate));

        return new ExerciseDay
        {
            DietPlanId = dietPlanId,
            UserId = userId,
            Date = date,
            Totals = ExerciseTotals.Zero(),
            Version = Guid.NewGuid()
        };
    }

    /// <summary>
    /// Records a session. A second session on the same date is added beside the first, never in
    /// place of it (FR-004).
    /// </summary>
    public ExerciseEntry AddEntry(
        Guid activityTypeId,
        string activityName,
        decimal met,
        int durationMinutes,
        decimal memberWeightKg,
        DateTime? asOf = null)
    {
        var now = asOf ?? DateTime.UtcNow;

        CheckRule(new DurationMustBePositiveRule(durationMinutes));
        CheckRule(new DurationWithinCeilingRule(durationMinutes));

        var estimate = Estimator.Estimate(met, durationMinutes, memberWeightKg);

        var entry = ExerciseEntry.Record(Id, activityTypeId, activityName, met, durationMinutes, estimate, now);

        _entries.Add(entry);
        Recalculate();

        Emit(new ExerciseLoggedEvent(Id, Date, durationMinutes, estimate));
        return entry;
    }

    public ExerciseEntry UpdateEntry(
        Guid entryId,
        Guid activityTypeId,
        string activityName,
        decimal met,
        int durationMinutes,
        decimal memberWeightKg)
    {
        var entry = _entries.SingleOrDefault(e => e.Id == entryId)
            ?? throw new DomainException("That session is not on this day");

        CheckRule(new DurationMustBePositiveRule(durationMinutes));
        CheckRule(new DurationWithinCeilingRule(durationMinutes));

        entry.Revise(
            activityTypeId,
            activityName,
            met,
            durationMinutes,
            Estimator.Estimate(met, durationMinutes, memberWeightKg));

        Recalculate();
        return entry;
    }

    /// <returns><c>false</c> when the session is not on this day.</returns>
    public bool RemoveEntry(Guid entryId)
    {
        var entry = _entries.SingleOrDefault(e => e.Id == entryId);
        if (entry is null)
            return false;

        _entries.Remove(entry);
        Recalculate();
        return true;
    }

    /// <summary>Ordered by when each session was recorded, for display.</summary>
    public IReadOnlyList<ExerciseEntry> EntriesInOrder() => [.. _entries.OrderBy(e => e.RecordedAt)];

    /// <summary>
    /// Totals and the concurrency token move together on every mutation. Keeping them in one
    /// place is what makes the stored total safe to trust.
    /// </summary>
    private void Recalculate()
    {
        Totals = ExerciseTotals.Create(
            _entries.Sum(e => e.DurationMinutes),
            _entries.Sum(e => e.EstimatedKcal));

        Version = Guid.NewGuid();
        SetUpdated();
    }
}
