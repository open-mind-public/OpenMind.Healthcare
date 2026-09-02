using DDD.BuildingBlocks;
using DietApi.Features.Weight.DeleteWeightReading;
using DietApi.Features.Weight.GetWeightTrend;
using DietApi.Features.Weight.RecordWeight;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DietApi.Features.Weight;

public static class WeightEndpoints
{
    public static void MapWeightEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weight")
            .WithTags("Weight")
            .RequireAuthorization();

        group.MapGet("/", GetTrend)
            .WithName("GetWeightTrend")
            .WithOpenApi();

        group.MapPut("/{date}", Record)
            .WithName("RecordWeight")
            .WithOpenApi();

        group.MapDelete("/{date}", Delete)
            .WithName("DeleteWeightReading")
            .WithOpenApi();
    }

    private static async Task<IResult> GetTrend(
        IMediator mediator,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null)
    {
        var trend = await mediator.Send(new GetWeightTrendQuery(from, to));
        return trend is null ? Results.NotFound() : Results.Ok(trend);
    }

    private static async Task<IResult> Record(
        DateOnly date,
        [FromBody] RecordWeightRequest request,
        IMediator mediator)
    {
        try
        {
            var trend = await mediator.Send(new RecordWeightCommand(date, request));
            return Results.Ok(trend);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> Delete(DateOnly date, IMediator mediator)
    {
        try
        {
            var removed = await mediator.Send(new DeleteWeightReadingCommand(date));
            return removed ? Results.NoContent() : Results.NotFound();
        }
        catch (DomainException ex)
        {
            // Includes the refusal to delete a plan's only remaining reading.
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
