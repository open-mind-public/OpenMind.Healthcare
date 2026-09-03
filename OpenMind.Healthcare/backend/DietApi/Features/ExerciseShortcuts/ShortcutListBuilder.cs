using DietApi.Domain.Aggregates;
using DietPlanAggregate = DietApi.Domain.Aggregates.DietPlan;
using DietApi.Domain.Repositories;

namespace DietApi.Features.ExerciseShortcuts;

/// <summary>
/// Turns a plan's shortcuts into the list every one of these endpoints returns.
/// </summary>
/// <remarks>
/// Every shortcut endpoint answers with the whole list, so a client updates in one round trip
/// rather than reconciling a patch. Resolving the activity names is the only part that needs the
/// catalogue, and doing it in one place keeps five handlers from each growing their own copy.
/// </remarks>
public class ShortcutListBuilder(IActivityTypeRepository catalogue)
{
    public async Task<ExerciseShortcutListResponse> BuildAsync(
        DietPlanAggregate plan, CancellationToken cancellationToken = default)
    {
        var activities = new Dictionary<Guid, ActivityType>();

        foreach (var id in plan.ExerciseShortcuts.Select(s => s.ActivityTypeId).Distinct())
        {
            var activity = await catalogue.GetByIdAsync(id, cancellationToken);

            if (activity is not null)
                activities[id] = activity;
        }

        return ExerciseShortcutMapper.ToList(plan, activities);
    }
}
