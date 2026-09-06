using DDD.BuildingBlocks;
using DietApi.Features.BeerDays.GetBeerDayRange;
using DietApi.Features.BeerDays.MarkBeerDay;
using DietApi.Features.BeerDays.UnmarkBeerDay;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Features.BeerDays;

public static class BeerDaysEndpoints
{
    public static void MapBeerDaysEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/beer-days")
            .WithTags("BeerDays")
            .RequireAuthorization();

        // Deliberately a separate endpoint from /api/food-log: the eating contract stays unaware of
        // beer, and the calendar fetches this range and merges it, the same way it does exercise.
        group.MapGet("/", GetRange)
            .WithName("GetBeerDayRange")
            .WithOpenApi();

        group.MapPut("/{date}", Mark)
            .WithName("MarkBeerDay")
            .WithOpenApi();

        group.MapDelete("/{date}", Unmark)
            .WithName("UnmarkBeerDay")
            .WithOpenApi();
    }

    private static async Task<IResult> GetRange(
        IMediator mediator,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
    {
        var range = await mediator.Send(new GetBeerDayRangeQuery(from, to));
        return range is null ? Results.NotFound() : Results.Ok(range);
    }

    private static async Task<IResult> Mark(DateOnly date, IMediator mediator)
    {
        try
        {
            var result = await mediator.Send(new MarkBeerDayCommand(date));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (DbUpdateException)
        {
            // Two devices marked the same day at once and the unique index caught the second.
            // The date is a beer day either way - converge on that rather than surface an error.
            return Results.Ok(new BeerDayResponse(date, IsBeerDay: true));
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> Unmark(DateOnly date, IMediator mediator)
    {
        var result = await mediator.Send(new UnmarkBeerDayCommand(date));
        return result is null ? Results.NotFound() : Results.NoContent();
    }
}
