using DDD.BuildingBlocks;
using DietApi.Domain.ValueObjects;
using DietApi.Features.DietAnalytics.GetEatingPatterns;
using DietApi.Features.DietAnalytics.GetIntakeAnalysis;
using DietApi.Features.DietAnalytics.GetIntakeTrend;
using DietApi.Features.DietAnalytics.GetObservations;
using DietApi.Features.DietAnalytics.GetMacroAnalysis;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DietApi.Features.DietAnalytics;

/// <summary>
/// Read-only analytics over a member's own diet history.
/// </summary>
/// <remarks>
/// Every route here is a <c>GET</c>, and that is a guarantee rather than an accident: viewing
/// analytics cannot change anything, so there is no verb here that could (FR-024).
/// </remarks>
public static class DietAnalyticsEndpoints
{
    public static void MapDietAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/diet-analytics")
            .WithTags("DietAnalytics")
            .RequireAuthorization();

        group.MapGet("/intake", GetIntake)
            .WithName("GetIntakeAnalysis")
            .WithOpenApi();

        group.MapGet("/trend", GetTrend)
            .WithName("GetIntakeTrend")
            .WithOpenApi();

        group.MapGet("/macros", GetMacros)
            .WithName("GetMacroAnalysis")
            .WithOpenApi();

        group.MapGet("/patterns", GetPatterns)
            .WithName("GetEatingPatterns")
            .WithOpenApi();

        group.MapGet("/observations", GetObservations)
            .WithName("GetObservations")
            .WithOpenApi();
    }

    private static async Task<IResult> GetIntake(
        IMediator mediator,
        [FromQuery] PeriodPreset period = PeriodPreset.Month)
    {
        try
        {
            var analysis = await mediator.Send(new GetIntakeAnalysisQuery(period));
            return analysis is null ? Results.NotFound() : Results.Ok(analysis);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GetMacros(
        IMediator mediator,
        [FromQuery] PeriodPreset period = PeriodPreset.Month)
    {
        try
        {
            var analysis = await mediator.Send(new GetMacroAnalysisQuery(period));
            return analysis is null ? Results.NotFound() : Results.Ok(analysis);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GetPatterns(
        IMediator mediator,
        [FromQuery] PeriodPreset period = PeriodPreset.Month,
        [FromQuery] int utcOffsetMinutes = 0)
    {
        try
        {
            var patterns = await mediator.Send(new GetEatingPatternsQuery(period, utcOffsetMinutes));
            return patterns is null ? Results.NotFound() : Results.Ok(patterns);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GetObservations(
        IMediator mediator,
        [FromQuery] PeriodPreset period = PeriodPreset.Month,
        [FromQuery] int utcOffsetMinutes = 0)
    {
        try
        {
            var observations = await mediator.Send(new GetObservationsQuery(period, utcOffsetMinutes));
            return observations is null ? Results.NotFound() : Results.Ok(observations);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GetTrend(
        IMediator mediator,
        [FromQuery] PeriodPreset period = PeriodPreset.Month)
    {
        try
        {
            var trend = await mediator.Send(new GetIntakeTrendQuery(period));
            return trend is null ? Results.NotFound() : Results.Ok(trend);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
