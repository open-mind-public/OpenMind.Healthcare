using DDD.BuildingBlocks;

namespace DietApi.Domain.Entities;

/// <summary>
/// One session of activity on an exercise day. Part of the <c>ExerciseDay</c> aggregate.
/// </summary>
/// <remarks>
/// The activity's name, its MET value and the resulting estimate are <em>snapshotted</em> at the
/// moment of recording. The catalogue id is kept for provenance but never re-read to recompute
/// anything, so correcting a MET value in the catalogue - or stepping on the scales tomorrow -
/// cannot rewrite a figure the member has already seen (FR-009).
/// </remarks>
public class ExerciseEntry : Entity
{
    public Guid ExerciseDayId { get; private set; }

    /// <summary>Provenance only. Never read back to recompute an estimate.</summary>
    public Guid ActivityTypeId { get; private set; }

    public string ActivityName { get; private set; } = string.Empty;
    public decimal Met { get; private set; }
    public int DurationMinutes { get; private set; }
    public int EstimatedKcal { get; private set; }
    public DateTime RecordedAt { get; private set; }

    // Private parameterless constructor for EF Core
    private ExerciseEntry() { }

    internal static ExerciseEntry Record(
        Guid exerciseDayId,
        Guid activityTypeId,
        string activityName,
        decimal met,
        int durationMinutes,
        int estimatedKcal,
        DateTime recordedAt) =>
        new()
        {
            ExerciseDayId = exerciseDayId,
            ActivityTypeId = activityTypeId,
            ActivityName = activityName,
            Met = met,
            DurationMinutes = durationMinutes,
            EstimatedKcal = estimatedKcal,
            RecordedAt = recordedAt
        };

    /// <summary>
    /// Re-snapshots the activity and its estimate. A member's own edit is a deliberate act,
    /// unlike a background correction to the catalogue, so it picks up the current values.
    /// </summary>
    internal void Revise(Guid activityTypeId, string activityName, decimal met, int durationMinutes, int estimatedKcal)
    {
        ActivityTypeId = activityTypeId;
        ActivityName = activityName;
        Met = met;
        DurationMinutes = durationMinutes;
        EstimatedKcal = estimatedKcal;
    }
}
