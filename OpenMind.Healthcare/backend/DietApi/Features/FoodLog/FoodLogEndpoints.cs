using DDD.BuildingBlocks;
using DietApi.Domain;
using DietApi.Features.FoodLog.AddFoodEntry;
using DietApi.Features.FoodLog.DeleteFoodEntry;
using DietApi.Features.FoodLog.GetDay;
using DietApi.Features.FoodLog.GetDayRange;
using DietApi.Features.FoodLog.UpdateFoodEntry;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DietApi.Features.FoodLog;

public static class FoodLogEndpoints
{
    public static void MapFoodLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/food-log")
            .WithTags("FoodLog")
            .RequireAuthorization();

        group.MapGet("/", GetRange)
            .WithName("GetLoggedDayRange")
            .WithOpenApi();

        group.MapGet("/{date}", GetDay)
            .WithName("GetLoggedDay")
            .WithOpenApi();

        group.MapPost("/{date}/entries", AddEntry)
            .WithName("AddFoodEntry")
            .WithOpenApi();

        group.MapPut("/entries/{entryId:guid}", UpdateEntry)
            .WithName("UpdateFoodEntry")
            .WithOpenApi();

        group.MapDelete("/entries/{entryId:guid}", DeleteEntry)
            .WithName("DeleteFoodEntry")
            .WithOpenApi();
    }

    private static async Task<IResult> GetRange(
        IMediator mediator,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
    {
        var range = await mediator.Send(new GetDayRangeQuery(from, to));
        return range is null ? Results.NotFound() : Results.Ok(range);
    }

    private static async Task<IResult> GetDay(DateOnly date, IMediator mediator)
    {
        try
        {
            var day = await mediator.Send(new GetDayQuery(date));
            return day is null ? Results.NotFound() : Results.Ok(day);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> AddEntry(
        DateOnly date,
        [FromBody] AddFoodEntryRequest request,
        IMediator mediator)
    {
        try
        {
            var day = await mediator.Send(new AddFoodEntryCommand(date, request));
            return Results.Ok(day);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateEntry(
        Guid entryId,
        [FromBody] UpdateFoodEntryRequest request,
        IMediator mediator)
    {
        try
        {
            var day = await mediator.Send(new UpdateFoodEntryCommand(entryId, request));
            return day is null ? Results.NotFound() : Results.Ok(day);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteEntry(
        Guid entryId,
        IMediator mediator,
        [FromQuery] Guid version)
    {
        try
        {
            var result = await mediator.Send(new DeleteFoodEntryCommand(entryId, version));

            if (!result.Found)
                return Results.NotFound();

            // No day left once its last entry goes - the date reverts to not logged.
            return result.Day is null ? Results.NoContent() : Results.Ok(result.Day);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }
}
