using DietApi.Domain.Aggregates;
using DietApi.Domain.ValueObjects;

namespace DietApi.Features.ActivityCatalogue;

public record ActivityTypeDto(Guid Id, string Name, ActivityCategory Category, decimal Met);

/// <summary>
/// An empty <c>Matches</c> is how a member learns the activity is not in the catalogue. The
/// client says so plainly rather than offering to invent an entry with no energy rate (FR-027).
/// </summary>
public record ActivitySearchResponse(string Query, IReadOnlyList<ActivityTypeDto> Matches);

public static class ActivityCatalogueMapper
{
    public static ActivityTypeDto ToDto(ActivityType activity) =>
        new(activity.Id, activity.Name, activity.Category, activity.Met);
}
