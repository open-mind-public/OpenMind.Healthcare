using DietApi.Domain.ValueObjects;
using DietApi.Features.DietGuidance.GetDailyEncouragement;
using DietApi.Features.DietGuidance.GetEatingTips;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DietApi.Features.DietGuidance;

public static class DietGuidanceEndpoints
{
    public static void MapDietGuidanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/diet-guidance")
            .WithTags("DietGuidance")
            .RequireAuthorization();

        group.MapGet("/tips", GetTips)
            .WithName("GetEatingTips")
            .WithOpenApi();

        group.MapGet("/encouragement", GetEncouragement)
            .WithName("GetDailyEncouragement")
            .WithOpenApi();
    }

    private static async Task<IResult> GetTips(IMediator mediator, [FromQuery] TipCategory? category = null)
    {
        var tips = await mediator.Send(new GetEatingTipsQuery(category));
        return Results.Ok(tips);
    }

    private static async Task<IResult> GetEncouragement(IMediator mediator)
    {
        var encouragement = await mediator.Send(new GetDailyEncouragementQuery());
        return encouragement is null ? Results.NotFound() : Results.Ok(encouragement);
    }
}
