using DietApi.Features.ActivityCatalogue.SearchActivities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DietApi.Features.ActivityCatalogue;

public static class ActivityCatalogueEndpoints
{
    public static void MapActivityCatalogueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/activity-catalogue")
            .WithTags("ActivityCatalogue")
            .RequireAuthorization();

        group.MapGet("/search", Search)
            .WithName("SearchActivities")
            .WithOpenApi();
    }

    private static async Task<IResult> Search(
        IMediator mediator,
        [FromQuery] string q = "",
        [FromQuery] int limit = 20)
    {
        // No match is an empty list, not a 404: the member asked a reasonable question and the
        // answer is that we do not have it (FR-027).
        var result = await mediator.Send(new SearchActivitiesQuery(q, limit));
        return Results.Ok(result);
    }
}
