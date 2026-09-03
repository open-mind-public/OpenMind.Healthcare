using DDD.BuildingBlocks;

namespace DietApi.Domain.Entities;

/// <summary>
/// A saved way to record a session in one tap. Part of the <c>DietPlan</c> aggregate.
/// </summary>
/// <remarks>
/// <para>
/// A shortcut <em>references</em> an activity; it does not copy one. There is deliberately no MET
/// value, no activity name and no energy estimate here. Those are resolved when the session is
/// actually recorded, from the activity's current energy rate and the member's current weight
/// (FR-010).
/// </para>
/// <para>
/// This is the mirror image of the snapshotting rule that governs a recorded session, and the
/// difference is the point. A session is a record of something that happened, so its figures are
/// frozen at the moment the member saw them. A shortcut is an instruction to record in future, so
/// freezing anything would guarantee it is stale by the time it is used - a member who lost ten
/// kilograms would go on getting estimates for the person they used to be, from a button that gives
/// no hint it is out of date.
/// </para>
/// </remarks>
public class ExerciseShortcut : Entity
{
    public Guid DietPlanId { get; private set; }

    /// <summary>A reference, never a copy. Resolved on read for display and on record for the estimate.</summary>
    public Guid ActivityTypeId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int DurationMinutes { get; private set; }

    /// <summary>Where it sits in the member's chosen order. Assigned only by the aggregate.</summary>
    public int Position { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Private parameterless constructor for EF Core
    private ExerciseShortcut() { }

    internal static ExerciseShortcut Save(
        Guid dietPlanId, Guid activityTypeId, string name, int durationMinutes, int position, DateTime createdAt) =>
        new()
        {
            DietPlanId = dietPlanId,
            ActivityTypeId = activityTypeId,
            Name = name.Trim(),
            DurationMinutes = durationMinutes,
            Position = position,
            CreatedAt = createdAt
        };

    internal void Rename(string name) => Name = name.Trim();

    internal void MoveTo(int position) => Position = position;

    /// <summary>
    /// Two shortcuts are the same when they record the same thing. The name is not part of it: two
    /// differently named buttons that both record a 45 minute run are exactly the duplication
    /// FR-006 exists to prevent, and renaming can therefore never create a duplicate.
    /// </summary>
    internal bool Records(Guid activityTypeId, int durationMinutes) =>
        ActivityTypeId == activityTypeId && DurationMinutes == durationMinutes;
}
