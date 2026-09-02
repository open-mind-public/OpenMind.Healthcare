using DietApi.Features.DietStats.GetDietStats;
using MediatR;

namespace DietApi.Features.DietStats;

public static class DietStatsEndpoints
{
    public static void MapDietStatsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/diet-stats")
            .WithTags("DietStats")
            .RequireAuthorization();

        group.MapGet("/", GetStats)
            .WithName("GetDietStats")
            .WithOpenApi();
    }

    private static async Task<IResult> GetStats(IMediator mediator)
    {
        var stats = await mediator.Send(new GetDietStatsQuery());
        return stats is null ? Results.NotFound() : Results.Ok(stats);
    }
}
