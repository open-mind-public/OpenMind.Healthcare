using DietApi.Domain.Aggregates;
using DietPlanAggregate = DietApi.Domain.Aggregates.DietPlan;
using DietApi.Domain.Entities;

namespace DietApi.Features.ExerciseShortcuts;

/// <summary>
/// A saved way to record a session.
/// </summary>
/// <remarks>
/// <c>ActivityName</c> is resolved from the catalogue on read rather than stored, so a corrected
/// activity name shows up here — which is right, because this is a button and not a record. There
/// is deliberately no MET and no estimate: the figure is computed when the session is recorded,
/// from the member's current weight (FR-010).
/// </remarks>
public record ExerciseShortcutDto(
    Guid Id,
    string Name,
    Guid ActivityTypeId,
    string ActivityName,
    int DurationMinutes,
    int Position,
    bool Available);

/// <summary>
/// A member's shortcuts, in their order.
/// </summary>
/// <remarks>
/// <c>RemainingSlots</c> lets a client say how many more may be added before the limit, rather than
/// the member discovering it on a failed save (FR-007).
/// </remarks>
public record ExerciseShortcutListResponse(
    IReadOnlyList<ExerciseShortcutDto> Shortcuts,
    int MaxShortcuts,
    int RemainingSlots);

public record CreateShortcutRequest(Guid ActivityTypeId, int DurationMinutes, string? Name);

public record RenameShortcutRequest(string Name);

/// <summary>
/// The <em>complete</em> ordered list, not a move.
/// </summary>
/// <remarks>
/// A full-list reorder is idempotent and has no race: two clients sending different orders produce
/// one of the two, never an interleaving. Move-up and move-down against stale positions produce
/// orders neither client asked for.
/// </remarks>
public record ReorderShortcutsRequest(IReadOnlyList<Guid> OrderedIds);


public static class ExerciseShortcutMapper
{
    /// <summary>
    /// The default name for a new shortcut, so a member never has to name one to use it (FR-004).
    /// </summary>
    public static string DefaultName(string activityName, int durationMinutes) =>
        $"{activityName}, {durationMinutes} min";

    public static ExerciseShortcutDto ToDto(ExerciseShortcut shortcut, ActivityType? activity) =>
        new(shortcut.Id,
            shortcut.Name,
            shortcut.ActivityTypeId,

            // An activity that has left the catalogue leaves the shortcut unusable rather than
            // silently recording something else (FR-013).
            activity?.Name ?? "No longer available",
            shortcut.DurationMinutes,
            shortcut.Position,
            Available: activity is not null);

    public static ExerciseShortcutListResponse ToList(
        DietPlanAggregate plan, IReadOnlyDictionary<Guid, ActivityType> activities) =>
        new([.. plan.ShortcutsInOrder().Select(s => ToDto(s, activities.GetValueOrDefault(s.ActivityTypeId)))],
            DietPlanAggregate.MaxShortcuts,
            plan.RemainingShortcutSlots);
}
