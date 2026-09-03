using DDD.BuildingBlocks;
using DietApi.Domain;
using DietApi.Features.Exercise.AddExerciseEntry;
using DietApi.Features.Exercise.DeleteExerciseEntry;
using DietApi.Features.Exercise.GetActivitySummary;
using DietApi.Features.Exercise.GetExerciseDay;
using DietApi.Features.Exercise.GetExerciseRange;
using DietApi.Features.Exercise.UpdateExerciseEntry;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DietApi.Features.Exercise;

public static class ExerciseEndpoints
{
    public static void MapExerciseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exercise")
            .WithTags("Exercise")
            .RequireAuthorization();

        // Deliberately a separate endpoint from /api/food-log: the eating contract stays unaware
        // of exercise, and the calendar fetches both and merges (research.md R-005, FR-013).
        group.MapGet("/", GetRange)
            .WithName("GetExerciseRange")
            .WithOpenApi();

        // Ahead of the {date} route in the file for readability; ASP.NET Core routing prefers
        // the literal segment regardless, so "summary" is never parsed as a date.
        group.MapGet("/summary", GetSummary)
            .WithName("GetActivitySummary")
            .WithOpenApi();

        group.MapGet("/{date}", GetDay)
            .WithName("GetExerciseDay")
            .WithOpenApi();

        group.MapPost("/{date}/entries", AddEntry)
            .WithName("AddExerciseEntry")
            .WithOpenApi();

        group.MapPut("/entries/{entryId:guid}", UpdateEntry)
            .WithName("UpdateExerciseEntry")
            .WithOpenApi();

        group.MapDelete("/entries/{entryId:guid}", DeleteEntry)
            .WithName("DeleteExerciseEntry")
            .WithOpenApi();
    }

    private static async Task<IResult> GetRange(
        IMediator mediator,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
    {
        var range = await mediator.Send(new GetExerciseRangeQuery(from, to));
        return range is null ? Results.NotFound() : Results.Ok(range);
    }

    private static async Task<IResult> GetSummary(IMediator mediator)
    {
        var summary = await mediator.Send(new GetActivitySummaryQuery());
        return summary is null ? Results.NotFound() : Results.Ok(summary);
    }

    private static async Task<IResult> GetDay(DateOnly date, IMediator mediator)
    {
        try
        {
            var day = await mediator.Send(new GetExerciseDayQuery(date));
            return day is null ? Results.NotFound() : Results.Ok(day);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> AddEntry(
        DateOnly date,
        [FromBody] AddExerciseEntryRequest request,
        IMediator mediator)
    {
        try
        {
            // Null means the activity is not in the catalogue - a 404 about the activity, not
            // about the day, which exists either way.
            var day = await mediator.Send(new AddExerciseEntryCommand(date, request));
            return day is null ? Results.NotFound(new { message = "That activity is not in the catalogue" }) : Results.Ok(day);
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
        [FromBody] UpdateExerciseEntryRequest request,
        IMediator mediator)
    {
        try
        {
            var day = await mediator.Send(new UpdateExerciseEntryCommand(entryId, request));
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
            var result = await mediator.Send(new DeleteExerciseEntryCommand(entryId, version));

            if (!result.Found)
                return Results.NotFound();

            // No day left once its last session goes - the date reverts to no exercise recorded.
            return result.Day is null ? Results.NoContent() : Results.Ok(result.Day);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }
}
