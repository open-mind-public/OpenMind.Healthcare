using DDD.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuitSmokingApi.Features.SmokedDays.GetRelapseAnalytics;
using QuitSmokingApi.Features.SmokedDays.GetSmokedDays;
using QuitSmokingApi.Features.SmokedDays.MarkDayAsSmoked;
using QuitSmokingApi.Features.SmokedDays.UnmarkSmokedDay;

namespace QuitSmokingApi.Features.SmokedDays;

public static class SmokedDaysEndpoints
{
    public static void MapSmokedDaysEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/smoked-days")
            .WithTags("SmokedDays")
            .RequireAuthorization();

        group.MapGet("/", GetSmokedDays)
            .WithName("GetSmokedDays")
            .WithOpenApi();

        group.MapPost("/", MarkDayAsSmoked)
            .WithName("MarkDayAsSmoked")
            .WithOpenApi();

        group.MapDelete("/{date}", UnmarkSmokedDay)
            .WithName("UnmarkSmokedDay")
            .WithOpenApi();

        group.MapGet("/analytics", GetRelapseAnalytics)
            .WithName("GetRelapseAnalytics")
            .WithOpenApi();
    }

    private static async Task<IResult> GetSmokedDays(
        IMediator mediator,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null)
    {
        var days = await mediator.Send(new GetSmokedDaysQuery(from, to));
        return Results.Ok(days);
    }

    private static async Task<IResult> MarkDayAsSmoked(
        [FromBody] MarkSmokedDayRequest request,
        IMediator mediator)
    {
        try
        {
            var command = new MarkDayAsSmokedCommand(
                request.Date,
                request.CigarettesSmoked,
                request.Trigger,
                request.Note);

            var smokedDay = await mediator.Send(command);
            return Results.Ok(smokedDay);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UnmarkSmokedDay(DateOnly date, IMediator mediator)
    {
        var removed = await mediator.Send(new UnmarkSmokedDayCommand(date));
        return removed ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> GetRelapseAnalytics(IMediator mediator)
    {
        var analytics = await mediator.Send(new GetRelapseAnalyticsQuery());
        return Results.Ok(analytics);
    }
}
