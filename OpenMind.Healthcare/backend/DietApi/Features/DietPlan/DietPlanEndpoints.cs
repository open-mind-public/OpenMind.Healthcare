using DDD.BuildingBlocks;
using DietApi.Features.DietPlan.CreateDietPlan;
using DietApi.Features.DietPlan.GetDietPlan;
using DietApi.Features.DietPlan.SetTargets;
using DietApi.Features.DietPlan.SuggestTargets;
using DietApi.Features.DietPlan.UpdateDietPlan;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DietApi.Features.DietPlan;

public static class DietPlanEndpoints
{
    public static void MapDietPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/diet-plan")
            .WithTags("DietPlan")
            .RequireAuthorization();

        group.MapGet("/", GetPlan)
            .WithName("GetDietPlan")
            .WithOpenApi();

        group.MapPost("/target-suggestion", SuggestTargets)
            .WithName("SuggestDietTargets")
            .WithOpenApi();

        group.MapPost("/", CreatePlan)
            .WithName("CreateDietPlan")
            .WithOpenApi();

        group.MapPut("/", UpdatePlan)
            .WithName("UpdateDietPlan")
            .WithOpenApi();

        group.MapPut("/targets", SetTargets)
            .WithName("SetDietTargets")
            .WithOpenApi();
    }

    private static async Task<IResult> GetPlan(IMediator mediator)
    {
        var plan = await mediator.Send(new GetDietPlanQuery());
        return plan is null ? Results.NotFound() : Results.Ok(plan);
    }

    private static async Task<IResult> SuggestTargets(
        [FromBody] SuggestTargetsRequest request,
        IMediator mediator)
    {
        try
        {
            var suggestion = await mediator.Send(new SuggestTargetsQuery(request));
            return Results.Ok(suggestion);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> CreatePlan(
        [FromBody] CreateDietPlanRequest request,
        IMediator mediator)
    {
        try
        {
            var response = await mediator.Send(new CreateDietPlanCommand(request));
            return Results.Created("/api/diet-plan", response);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdatePlan(
        [FromBody] UpdateDietPlanRequest request,
        IMediator mediator)
    {
        try
        {
            var response = await mediator.Send(new UpdateDietPlanCommand(request));
            return Results.Ok(response);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SetTargets(
        [FromBody] SetTargetsRequest request,
        IMediator mediator)
    {
        try
        {
            var response = await mediator.Send(new SetDietTargetsCommand(request));
            return Results.Ok(response);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
